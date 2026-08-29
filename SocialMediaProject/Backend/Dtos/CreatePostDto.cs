public class CreatePostDto
{
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }

    public List<IFormFile> Media { get; set; } = new();

    public string? Videos { get; set; }
}