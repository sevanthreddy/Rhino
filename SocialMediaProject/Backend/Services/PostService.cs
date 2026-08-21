using Backend.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;

    private readonly INotificationService _notificationService;

    private readonly ServiceBusPublisher _servicebusPublisher;
    private readonly ILogger<PostService> _logger;
    private readonly BlobStorageService _blobStorage;


    public PostService(ApplicationDbContext context,INotificationService notificationService,ServiceBusPublisher serviceBusPublisher,BlobStorageService blobStorageService,ILogger<PostService> logger)
    {
        _context = context;
        _notificationService=notificationService;
        _servicebusPublisher=serviceBusPublisher;
        _logger=logger;
        _blobStorage=blobStorageService;
    }

public async Task<IEnumerable<PostDto>> GetPostsAsync(int userid)
{
    try
    {
        var result = await _context.Posts
            .ToListAsync();

        _logger.LogInformation("Posts loaded: {Count}", result.Count);

        return result.Select(p => new PostDto
        {
            Id = p.Id,
            Content = p.Content,
            CreatedAt = p.CreatedAt,
            UserId = p.UserId
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "GET POSTS FAILED");
        throw;
    }
}
    


    public async Task<PostDto> GetPostByIdAsync(int userid, int postid)
    {
        var posts = await _context.Posts.Include(p=>p.User).Where(p => p.Id == postid).FirstOrDefaultAsync();
        if (posts == null)
        {
            return null;
        }
        else
        {
            var datattorontend = new PostDto
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
            };
            return datattorontend;
        }
    }

    public async Task<Post> CreatePostAsync(CreatePostDto createPostDto)
    {
        var post = new Post
        {
            Content = createPostDto.Content,
            CreatedAt = DateTime.UtcNow,
            UserId = createPostDto.UserId
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        if (createPostDto.Images.Count > 0)
        {
            var images = new List<Images>();
            foreach (var image in createPostDto.Images)
            {
                var filename = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var imageUrl = await _blobStorage.UploadAsync(image, filename);
                images.Add(new Images
                {
                    postid = post.Id,
                    ImageURL = imageUrl
                });
                //var pathcombine = Path.Combine("wwwroot", "Uploads", filename);
                //var filestream = new FileStream(pathcombine, FileMode.Create);
                //await image.CopyToAsync(filestream);
                
            }
            _context.Images.AddRange(images);
            await _context.SaveChangesAsync();
        }

        return post;
    }

    public async Task<(int, bool)> LikePostAsync(int postid, int userid)
    {
        try
        {
            var likestatus = await _context.Likes.Where(p => p.Postid == postid && p.Userid == userid).AnyAsync();
            _logger.LogInformation("Like status for user {UserId} and post {PostId}: {LikeStatus}", userid, postid, likestatus);
            if (likestatus == true)
            {
                var likeobj = _context.Likes.Where(p => p.Postid == postid && p.Userid == userid);
                _context.RemoveRange(likeobj);
                await _context.SaveChangesAsync();
                int totallikes1 = await _context.Likes.Where(p => p.Postid == postid).CountAsync();
                return (totallikes1, true);
            }
            var like = new Likes
            {
                Postid = postid,
                Userid = userid,
                LikedAt = DateTime.UtcNow
            };
            _context.Likes.Add(like);

            await _context.SaveChangesAsync();
           
            await _servicebusPublisher.SendAsync(new NotificationDto
            {
                Content="Liked Your Post",
                Senderid=userid,
                IsRead=false,
                CreatedAt=DateTime.UtcNow,
                Type="Like",
                ReceiverId=await _context.Posts.Where(P=>P.Id==postid).Select(P=>P.UserId).FirstOrDefaultAsync()
                
            });

            int totallikes = await _context.Likes.Where(p => p.Postid == postid).CountAsync();
            return (totallikes, true);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "LIKE POST FAILED for post {PostId} and user {UserId}", postid, userid);
            return (0, false);
        }


    }
}