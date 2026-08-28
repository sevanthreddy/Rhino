using Backend.Dtos;
using Backend.Models;

namespace Backend.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterUserAsync(RegisterDto registerDto);
    Task<(bool Success, string Message, string? Token, string? RefreshToken)> LoginUserAsync(LoginDto loginDto);
    string GenerateJwtToken(User user);

    Task<(bool Success, string Message, string? NewAccessToken, string? NewRefreshToken)> RefreshTokenAsync(string refreshToken);

    Task<bool> RevokeRefreshTokenAsync(string refreshtoken);
    string GenerateRefreshToken();
}