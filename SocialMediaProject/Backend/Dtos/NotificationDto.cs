public class NotificationDto
{
     public int? Id { get; set; }
    public string Content { get; set; }=null!;
    public int Senderid { get; set; }
    public bool? IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = null!;
    public int ReceiverId{get;set;}

    public string? SenderUserName{get;set;}
}