public class PostDto
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string TimeAgo { get; set; } = string.Empty;

    public int LikeCount { get; set; }

    public bool IsLiked { get; set; } = false;

    public List<string> ImagesRelatedtoPost { get; set; } = null!;

    public string? profileImage { get; set; } = null!;
}