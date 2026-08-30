using Backend.Data;
using Backend.Dtos;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;


namespace Backend.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(RegisterDto registerDto)
    {
        var validationMessage = ValidateRegistration(registerDto);

        if (validationMessage != null)
        {
            _logger.LogWarning(
                "Registration validation failed: {Message}",
                validationMessage);

            return (false, validationMessage);
        }
        // 1. Check Username
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username.Trim().ToLower()))
        {
            _logger.LogWarning("Registration rejected because username already exists: {Username}", registerDto.Username);
            return (false, "Username already exists");
        }

        // 2. Check Email
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email.ToLower().Trim()))
        {
            _logger.LogWarning("Registration rejected because email already exists: {Email}", registerDto.Email);
            return (false, "Email address already exists");
        }
        string securePasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password, workFactor: 12);
        // 3. Save to Database
        _context.Users.Add(new User
        {
            Username = registerDto.Username.Trim().ToLower(),
            Email = registerDto.Email.ToLower().Trim(),
            PasswordHash = securePasswordHash
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Username}", registerDto.Username);
        return (true, "User registered successfully");
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> LoginUserAsync(LoginDto loginDto)
    {
        // Check if the user exists by username or email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Identifier.Trim().ToLower() || u.Email == loginDto.Identifier.ToLower().Trim());

        if (user == null)
        {
            _logger.LogWarning("Login rejected for unknown identifier: {Identifier}", loginDto.Identifier);
            return (false, "Invalid username/email or password", null, null);
        }

        // Check if the password matches
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login rejected because password verification failed for user {UserId}", user.Id);
            return (false, "Invalid username/email or password", null, null);
        }
        string refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
        return (true, "Login successful", GenerateJwtToken(user), user.RefreshToken);
    }

    public string GenerateJwtToken(Models.User user)
    {
        var claims = new[]
{
    new Claim("userId", user.Id.ToString()),
    new Claim("username", user.Username),
    new Claim("email", user.Email)
};

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretBackEndKeyPolymedicure123!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: creds);
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretBackEndKeyPolymedicure123!")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false // ⚠️ CRITICAL: Tells the engine NOT to throw an error if the token is expired!
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);

            // Safety check: Ensure the token was signed using the expected HMAC-SHA256 algorithm
            if (validatedToken is not System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null; // Return null if the token structure is completely corrupted or fake
        }
    }

    public async Task<(bool Success, string Message, string? NewAccessToken, string? NewRefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("refresh token from frontend:{0}", refreshToken);

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            // UPDLOCK+ROWLOCK: any other request trying to touch this same row
            // has to wait here until this transaction commits or rolls back.
            var user = await _context.Users
                .FromSqlInterpolated($@"
            SELECT * FROM Users WITH (UPDLOCK, ROWLOCK)
            WHERE RefreshToken = {refreshToken} OR PreviousRefreshToken = {refreshToken}")
                .FirstOrDefaultAsync();

            if (user == null)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Refresh rejected because the token was not associated with a user");
                return (false, "Invalid or expired refresh token session. Please log in again.", null, null);
            }
            _logger.LogInformation("refreshtoken from database {0}", user.RefreshToken);
            // Case 1: current token — rotate normally
            if (user.RefreshToken == refreshToken)
            {
                if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Refresh rejected because the session expired for user {UserId}", user.Id);
                    return (false, "Session expired. Please log in again.", null, null);
                }

                string newAccessToken = GenerateJwtToken(user);
                string newRefreshToken = GenerateRefreshToken();

                user.PreviousRefreshToken = user.RefreshToken;
                user.PreviousRefreshTokenExpiry = DateTime.UtcNow.AddSeconds(10);
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);
                return (true, "Token renewed successfully!", newAccessToken, newRefreshToken);
            }

            // Case 2: just-rotated-out token, within grace window — benign race
            if (user.PreviousRefreshToken == refreshToken && user.PreviousRefreshTokenExpiry > DateTime.UtcNow)
            {
                string newAccessToken = GenerateJwtToken(user);
                await transaction.CommitAsync();
                _logger.LogInformation("Refresh token grace window used for user {UserId}", user.Id);
                return (true, "Token renewed successfully!", newAccessToken, user.RefreshToken);
            }

            // Case 3: stale beyond grace window — kill the session
            user.RefreshToken = null;
            user.PreviousRefreshToken = null;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            _logger.LogWarning("Stale refresh token revoked and session cleared for user {UserId}", user.Id);
            return (false, "Invalid or expired refresh token session. Please log in again.", null, null);
        });
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

            if (user == null)
            {
                _logger.LogWarning("Refresh token revocation requested for an unknown session");
                return false;
            }

            // Revoke the refresh token by clearing token and resetting expiry to a past value
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.MinValue;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token revocation failed");
            return false;
        }
    }

    private string? ValidateRegistration(RegisterDto registerDto)
    {
        // -----------------------------------------
        // Username
        // -----------------------------------------

        if (string.IsNullOrWhiteSpace(registerDto.Username))
        {
            return "Username is required";
        }

        var username = registerDto.Username.Trim();

        if (username.Length < 3)
        {
            return "Username must be at least 3 characters";
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(
            username,
            @"^[a-zA-Z0-9_]+$"))
        {
            return "Username can only contain letters, numbers and _";
        }


        // -----------------------------------------
        // Email
        // -----------------------------------------

        if (string.IsNullOrWhiteSpace(registerDto.Email))
        {
            return "Email is required";
        }

        var email = registerDto.Email.Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
        {
            return "Please enter a valid email address";
        }


        // -----------------------------------------
        // Password
        // -----------------------------------------

        if (string.IsNullOrEmpty(registerDto.Password))
        {
            return "Password is required";
        }

        if (registerDto.Password.Length < 8)
        {
            return "Password must be at least 8 characters";
        }

        if (!registerDto.Password.Any(char.IsUpper))
        {
            return "Password must contain an uppercase letter";
        }

        if (!registerDto.Password.Any(char.IsLower))
        {
            return "Password must contain a lowercase letter";
        }

        if (!registerDto.Password.Any(char.IsDigit))
        {
            return "Password must contain a number";
        }

        if (!registerDto.Password.Any(
            c => !char.IsLetterOrDigit(c)))
        {
            return "Password must contain a special character";
        }


        // -----------------------------------------
        // Everything is valid
        // -----------------------------------------

        return null;
    }
}