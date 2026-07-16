namespace Backend.Dtos;

public class LoginDto
{
    // This can accept either the username OR email depending on your design preference
    public string Identifier { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty;
}