using Backend.Models;

public class Message
{
    public int MessageId{get;set;}
    public string Content{get;set;}=null!;
    public User Sender{get;set;}=null!;
    public int SenderId{get;set;}
    public int ReceiverId{get;set;}
    public User Receiver{get;set;}=null!;
    public DateTime SentAt{get;set;}
    public string Status{get;set;}=null!;

}