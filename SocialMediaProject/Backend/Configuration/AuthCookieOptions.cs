using Microsoft.AspNetCore.Http;

namespace Backend.Configuration;

public sealed class AuthCookieOptions
{
    public bool Secure { get; set; } = true;
    public SameSiteMode SameSite { get; set; } = SameSiteMode.None;
    public int ExpirationDays { get; set; } = 7;
}
