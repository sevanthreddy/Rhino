using Backend.Models;

public class Notifications
{
    public int Id { get; set; }
    public string Content { get; set; }=null!;
    public int Senderid { get; set; }
    public User Sender{get;set;}=null!;
    public bool? IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = null!;

    public int ReceiverId{get;set;}
    public User Receiver{get;set;}=null!;
}