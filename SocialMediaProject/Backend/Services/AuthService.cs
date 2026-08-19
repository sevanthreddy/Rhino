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

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> RegisterUserAsync(RegisterDto registerDto)
    {
        // 1. Check Username
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username.Trim().ToLower()))
        {
            return (false, "Username already exists");
        }

        // 2. Check Email
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email.ToLower().Trim()))
        {
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

        return (true, "User registered successfully");
    }

    public async Task<(bool Success, string Message, string? Token, string? RefreshToken)> LoginUserAsync(LoginDto loginDto)
    {
        // Check if the user exists by username or email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Identifier.Trim().ToLower() || u.Email == loginDto.Identifier.ToLower().Trim());

        if (user == null)
        {
            return (false, "Invalid username/email or password", null, null);
        }

        // Check if the password matches
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return (false, "Invalid username/email or password", null, null);
        }
        string refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
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

    private string GenerateRefreshToken()
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
    
    public async Task<(bool Success, string Message, string? NewAccessToken, string? NewRefreshToken)>RefreshTokenAsync(string refreshToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return (
                false,
                "Invalid or expired refresh token session. Please log in again.",
                null,
                null
            );
        }

        string newAccessToken = GenerateJwtToken(user);
        string newRefreshToken = GenerateRefreshToken();

        // Rotate refresh token
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return (
            true,
            "Token renewed successfully!",
            newAccessToken,
            newRefreshToken
        );
    }

   public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
{
    try
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

        if (user == null)
        {
            return false;
        }

        // Revoke the refresh token by clearing token and resetting expiry to a past value
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = DateTime.MinValue;

        await _context.SaveChangesAsync();

        return true;
    }
    catch
    {
        return false;
    }
}
}