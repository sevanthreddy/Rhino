using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ProfileImageURL { get; set; }
    public string? About { get; set; }
    public string? Name { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; set; }
    public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    public ICollection<Follow> Following { get; set; } = new List<Follow>();

    public ICollection<Message> SentMessages { get; set; }

    public ICollection<Message> ReceivedMessages { get; set; }

     public string? PreviousRefreshToken { get; set; }
    public DateTime? PreviousRefreshTokenExpiry { get; set; }

}