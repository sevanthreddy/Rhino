using System.Collections.Concurrent;

public class PresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<int,HashSet<string>> _onlineusers=new(); 
    private readonly ILogger<PresenceService> _logger;

    public PresenceService(ILogger<PresenceService> logger)
    {
        _logger = logger;
    }
    public async Task<bool> AddUserConnection(int userid,string connectionid)
    {
        try
        {
            var x=_onlineusers.GetOrAdd(userid,_=>new HashSet<string>());
            lock (x)
            {
                x.Add(connectionid);
                return true;
            }
        }
        catch(Exception e)
        {
            _logger.LogError(e, "ADD USER CONNECTION FAILED for user {UserId}", userid);
            return false;
        }
        
    }

    public async Task<List<int>> GetAllUsersOnline()
    {
        try
        {
            return _onlineusers.Keys.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET ONLINE USERS FAILED");
            return new List<int>();
        }
    }

    public async Task<bool> RemoveUserConnection(int userid, string connectionid)
    {
        try
        {
            if(!_onlineusers.TryGetValue(userid,out var connections))
            {
                return false;
            }
            lock (connections)
            {
                connections.Remove(connectionid);
                if (connections.Count() == 0)
                {
                    _onlineusers.TryRemove(userid,out _);
                }
                return true;
            }
        }
        catch(Exception e)
        {
            _logger.LogError(e, "REMOVE USER CONNECTION FAILED for user {UserId}", userid);
            return false;
            
        }
    }
}