public class MessageDto
{
    public int MessageId{get;set;}
    public string Content{get;set;}=null!;
    public int SenderId{get;set;}
    public int ReceiverId{get;set;}
    public DateTime SentAt{get;set;}
    public string Status{get;set;}=null!;
}