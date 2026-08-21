using Microsoft.AspNetCore.Mvc;
using Backend.Dtos;
using Backend.Services;
using Google.Apis.Auth;
using Backend.Models;
using Backend.Data;
using Backend.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterandLoginController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly AuthCookieOptions _cookieOptions;
    private readonly ILogger<RegisterandLoginController> _logger;

    // Inject the AuthService instead of the DbContext!
    public RegisterandLoginController(
        IAuthService authService,
        ApplicationDbContext context,
        IOptions<AuthCookieOptions> cookieOptions,
        ILogger<RegisterandLoginController> logger)
    {
        _authService = authService;
        _context = context;
        _cookieOptions = cookieOptions.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (registerDto == null)
        {
            return BadRequest("Invalid Request");
        }

        // Hand off all the database checks and heavy lifting to the service
        var (success, message) = await _authService.RegisterUserAsync(registerDto);

        if (!success)
        {
            return BadRequest(message);
        }

        return Ok(message);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        try
        {
            // Implement login logic here
            var (success, message, token, refreshToken) = await _authService.LoginUserAsync(loginDto);

            if (!success)
            {
                return BadRequest(message);
            }
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest("Refresh token was not generated.");
            }

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = _cookieOptions.Secure,
                SameSite = _cookieOptions.SameSite,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(_cookieOptions.ExpirationDays)
            });

            return Ok(new { Message = message, Token = token });

        }
        catch (Exception ex)
        {

            _logger.LogError(ex, "LOGIN FAILED");

            return StatusCode(500, new
            {
                message = ex.Message
            });
        }

    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshAccessToken()
    {
        try
        {
            _logger.LogInformation("RefreshAccessToken method started");
            var refreshtoken = Request.Cookies["refreshToken"];
            var refreshToken = Request.Cookies["refreshToken"];
            _logger.LogInformation("Refresh token cookie present: {HasRefreshToken}", !string.IsNullOrEmpty(refreshToken));
            if (string.IsNullOrEmpty(refreshtoken))
            {
                return Unauthorized("Refresh token not found.");
            }
            var res = await _authService.RefreshTokenAsync(refreshtoken);
            if (res.Success == true)
            {
                Response.Cookies.Append(
       "refreshToken",
       res.NewRefreshToken!,
       new CookieOptions
       {
           HttpOnly = true,
           Secure = _cookieOptions.Secure,
           SameSite = _cookieOptions.SameSite,
           Expires = DateTimeOffset.UtcNow.AddDays(_cookieOptions.ExpirationDays),
           Path = "/"
       }
   );
                _logger.LogInformation("RefreshAccessToken method completed successfully");
                return Ok(new
                {
                    success = res.Success,
                    message = res.Message,
                    newAccessToken = res.NewAccessToken
                });
            }
            else
            {
                return BadRequest(res);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "REFRESH TOKEN FAILED");
            return BadRequest();
        }


    }


    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        // Revoke refresh token in database here
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieOptions.Secure,
            SameSite = _cookieOptions.SameSite,
            Path = "/"
        });

        return Ok();
    }
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            //  1. Cryptographically verify the token with Google's public keys
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { "548377775699-jvaoa9mu4cvedtflbd9tb42in3m8icso.apps.googleusercontent.com" }
            };

            // If the token is fake, expired, or tampered with, this line will throw an exception
            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.Token, settings);

            //  2. Extract user identity claims from the verified Google payload
            string email = payload.Email;
            string name = payload.Name;

            //  3. Check if this email already exists in your SQL Server database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                //  4. If they don't exist, register them on-the-fly!
                user = new User
                {
                    Email = email,
                    Username = name.Replace(" ", "").ToLower(),
                    PasswordHash = "GOOGLE_AUTH_EXTERNAL",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Automatically registered Google user {Email}", email);
            }

            //  5. Generate your OWN application's custom JWT authentication token
            var myAppToken = _authService.GenerateJwtToken(user);

            //  6. Send the application token back to React
            return Ok(new { token = myAppToken });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google JWT received");
            return BadRequest("Invalid or tampered Google token configuration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GOOGLE LOGIN FAILED");
            return StatusCode(500, $"Internal server authentication error: {ex.Message}");
        }
    }
}