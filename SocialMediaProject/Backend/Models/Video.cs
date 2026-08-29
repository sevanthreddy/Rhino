public class Video
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public double Duration { get; set; }
}