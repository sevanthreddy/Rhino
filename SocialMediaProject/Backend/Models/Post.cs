using Backend.Models;

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Likes> Likes { get; set; } = new List<Likes>();

    public ICollection<Replies> Replies { get; set; } = new List<Replies>();

}