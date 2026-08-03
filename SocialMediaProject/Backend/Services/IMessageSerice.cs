public interface IMessageService
{
    Task<MessageDto> CreateMessageAsync(int senderid,string content,int receiverid);

    Task<IEnumerable<MessageDto>> GetMessagesBetweenUsersAsync(int senderid,int receiverid,int? lastmessageid=null,int numberofmessages=30);

    Task<IEnumerable<ChatInfoDto>> GetAllPrevChatsForAUserAsync(int mainid);

    Task<IEnumerable<ChatInfoDto>> GetAllUsers();

    Task<bool> UpdateStatusAsync(int messageid,string status);

    Task<IEnumerable<int>> UpdateStatusAllAsync(int senderid, int receiverid);

    Task<int> GetUnreadMessagesAsync(int userid);
}