using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Backend.Services;

public class FollowService : IFollowService
{
    public readonly ApplicationDbContext _context;

    public FollowService(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<bool> FollowUserAsync(int followerid, string username)
    {
        try
        {
            var TargetUser = await _context.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
            var followingid = TargetUser.Id;
            if (followerid == followingid)
            {
                return false;
            }

            var r = await _context.Follow.Where(f => f.FollowerId == followerid && f.FollowingId == followingid).FirstOrDefaultAsync();
            if (r != null)
            {
                _context.Follow.Remove(r);
                await _context.SaveChangesAsync();
                return true;
            }

            await _context.Follow.AddAsync(new Follow
            {
                FollowerId = followerid,
                FollowingId = followingid
            });
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
    public async Task<FollowInfoDto> GetFollowInfoAsync(int followerid, string username)
    {
        try
        {
            var TargetUser = await _context.Users.Where(u => u.Username == username).FirstOrDefaultAsync();
            var followingid = TargetUser.Id;
            var f = new FollowInfoDto()
            {
                followinfo = await _context.Follow.Where(f => f.FollowingId == followingid && f.FollowerId == followerid).AnyAsync(),
                totalposts = await _context.Posts.Where(p => p.UserId == followingid).CountAsync(),
                followercount = await _context.Follow.Where(f => f.FollowingId == followingid).CountAsync(),
                followingcount = await _context.Follow.Where(f => f.FollowerId == followingid).CountAsync(),
                about = await _context.Users.Where(u => u.Id == followingid).Select(u => u.About).FirstOrDefaultAsync(),
                profileImage = await _context.Users.Where(u => u.Id == followingid).Select(u => u.ProfileImageURL).FirstOrDefaultAsync()
            };
            return f;


        }
        catch
        {
            return new FollowInfoDto();
        }
    }

    public async Task<IEnumerable<PostDto>> GetClickedTabDataAsync(string username, string action)
    {
        try
        {
            var userid = await _context.Users.Where(u => u.Username == username).Select(u => u.Id).FirstOrDefaultAsync();
            if (action == "Posts")
            {
                var x = await _context.Posts
    .Where(p => p.UserId == userid)
    .Include(p => p.User)
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();
                var datattorontend = x.Select(posts => new PostDto
                {
                    Id = posts.Id,
                    Content = posts.Content,
                    CreatedAt = posts.CreatedAt,
                    UserId = posts.UserId,
                    TimeAgo = posts.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), // Format the timestamp as a string,
                    Initials = posts.User.Username.Length >= 2 ? posts.User.Username.Substring(0, 2).ToUpper() : posts.User.Username.ToUpper(), // Get the first two letters of the username in uppercase
                    Username = posts.User.Username, // Access the Username from the related User entity
                    LikeCount = _context.Likes.Where(p => p.Postid == posts.Id).Count(),
                    IsLiked = _context.Likes.Where(p => p.Postid == posts.Id && p.Userid == userid).Any(),
                    ImagesRelatedtoPost = _context.Images.Where(i => i.postid == posts.Id).Select(i => "uploads/" + i.ImageURL).ToList()
                });
                return datattorontend;
            }
            return Enumerable.Empty<PostDto>();
        }
        catch
        {
            return Enumerable.Empty<PostDto>();
        }
    }
}