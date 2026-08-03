public interface IPresenceService
{
    Task <bool> AddUserConnection(int userid,string connectionid);

    Task<bool> RemoveUserConnection(int userid,string connectionid);

    Task<List<int>> GetAllUsersOnline();
}