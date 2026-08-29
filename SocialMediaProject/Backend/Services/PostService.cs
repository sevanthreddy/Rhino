using Backend.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;

    private readonly INotificationService _notificationService;

    private readonly ServiceBusPublisher _servicebusPublisher;
    private readonly ILogger<PostService> _logger;
    private readonly BlobStorageService _blobStorage;



    public PostService(ApplicationDbContext context, INotificationService notificationService, ServiceBusPublisher serviceBusPublisher, BlobStorageService blobStorageService, ILogger<PostService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _servicebusPublisher = serviceBusPublisher;
        _logger = logger;
        _blobStorage = blobStorageService;
    }

    public async Task<IEnumerable<PostDto>> GetPostsAsync(int userid)
    {
        try
        {
            var posts = await _context.Posts
    .Include(p => p.User)
    .Include(p => p.Images)
    .Include(p => p.video)
    .Include(p => p.Likes)
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();

            var result = new List<PostDto>();

            foreach (var p in posts)
            {
                var videoUrls = new List<string>();

                foreach (var video in p.video)
                {
                    var videoUrl =
                        await _blobStorage.GenerateReadSasAsync(
                            video.BlobName
                        );

                    videoUrls.Add(videoUrl);
                }

                result.Add(new PostDto
                {
                    Id = p.Id,

                    Content = p.Content,

                    CreatedAt = p.CreatedAt,

                    UserId = p.UserId,

                    TimeAgo = p.CreatedAt.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    ),

                    Initials = p.User.Username.Length >= 2
                        ? p.User.Username
                            .Substring(0, 2)
                            .ToUpper()
                        : p.User.Username.ToUpper(),

                    Username = p.User.Username,

                    LikeCount = p.Likes.Count(),

                    IsLiked = p.Likes.Any(
                        l => l.Userid == userid
                    ),

                    ImagesRelatedtoPost = p.Images
                        .Select(i => i.ImageURL)
                        .ToList(),

                    profileImage = p.User.ProfileImageURL,

                    VideoURL = videoUrls.FirstOrDefault() ?? string.Empty
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET POSTS FAILED");
            throw;
        }
    }



    public async Task<PostDto> GetPostByIdAsync(int userid, int postid)
    {
        var posts = await _context.Posts.Include(p => p.User).Where(p => p.Id == postid).FirstOrDefaultAsync();
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


        // ==========================================
        // IMAGES
        // ==========================================

        if (createPostDto.Media.Count > 0)
        {
            var images = new List<Images>();

            foreach (var image in createPostDto.Media)
            {
                var filename =
                    Guid.NewGuid() + Path.GetExtension(image.FileName);

                var imageUrl =
                    await _blobStorage.UploadAsync(image, filename);

                images.Add(new Images
                {
                    postid = post.Id,
                    ImageURL = imageUrl
                });
            }

            _context.Images.AddRange(images);
        }


        // ==========================================
        // VIDEOS
        // ==========================================

        if (!string.IsNullOrEmpty(createPostDto.Videos))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var videoMetadata =
                JsonSerializer.Deserialize<List<VideoMetadataDto>>(
                    createPostDto.Videos,
                    options
                );

            if (videoMetadata != null && videoMetadata.Count > 0)
            {
                var videos = new List<Video>();

                foreach (var video in videoMetadata)
                {
                    videos.Add(new Video
                    {
                        PostId = post.Id,
                        BlobName = video.BlobName,
                        ContentType = video.ContentType,
                        FileSize = video.FileSize
                    });
                }

                _context.Video.AddRange(videos);
            }
        }


        // Save images + video metadata
        await _context.SaveChangesAsync();

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
                Content = "Liked Your Post",
                Senderid = userid,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = "Like",
                ReceiverId = await _context.Posts.Where(P => P.Id == postid).Select(P => P.UserId).FirstOrDefaultAsync()

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

    public async Task<string> GenerateVideoUploadSasAsync(
    string fileName,
    string contentType)
    {
        return await _blobStorage.GenerateUploadSasAsync(
            fileName,
            contentType);
    }
}