public class ReplyPostDto
{
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public int PostId { get; set; }

    public string Username { get; set; } = string.Empty;


    public List<string> ImagesRelatedtoPost { get; set; } = new();

    public string Initials{get;set;}=string.Empty;


}