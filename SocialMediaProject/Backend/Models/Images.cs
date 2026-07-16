public class Images
{
    public int id { get; set; }

    public string ImageURL { get; set; } = string.Empty;

    public int? postid { get; set; }

    public int? ReplyId { get; set; }

    public Post? Post { get; set; }

    public Replies? Reply { get; set; }
}