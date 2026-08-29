

namespace Backend.Services;

public interface IFollowService
{
     Task<bool> FollowUserAsync(int followerid,string username);

     Task<FollowInfoDto> GetFollowInfoAsync(int followerid,string username);

     Task<IEnumerable<object>> GetClickedTabDataAsync(string username,string action);

}