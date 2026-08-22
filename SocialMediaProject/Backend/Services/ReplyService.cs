using Backend.Data;
using Backend.Dtos;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

public class ReplyService : IReplyService
{
    private readonly ApplicationDbContext _context;

    private readonly ServiceBusPublisher _servicebusPublisher;

    private readonly ILogger<ReplyService> _logger;

public ReplyService(
    ApplicationDbContext applicationDbContext,
    ServiceBusPublisher serviceBusPublisher,
    ILogger<ReplyService> logger)
{
    _context = applicationDbContext;
    _servicebusPublisher = serviceBusPublisher;
    _logger = logger;
}

    public async Task<IEnumerable<ReplyPostDto>> GetReplyPostDtosAsync(int postid)
    {
        var x = await _context.Replies.Where(p => p.PostId == postid)
            .Include(p => p.User) // Include the User navigation property
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        var datattorontend = x.Select(reply => new ReplyPostDto
        {
            Content = reply.Content,
            CreatedAt = reply.CreatedAt,
            UserId = reply.UserId,
            Username = reply.User.Username, // Access the Username from the related User entity
            ImagesRelatedtoPost = _context.Images.Where(i => i.ReplyId == reply.Id).Select(i => "uploads/" + i.ImageURL).ToList()
        });

        return datattorontend;

    }

    public async Task<bool> CreateReplyAsync(CreateReplyPostDto createReplyPostDto)
    {
        try
        {
             var reply = new Replies
        {
            Content = createReplyPostDto.Content,
            CreatedAt = DateTime.Now,
            UserId = createReplyPostDto.UserId,
            PostId = createReplyPostDto.Postid
        };
        await _context.Replies.AddAsync(reply);
        var res = await _context.SaveChangesAsync();
        var z=await _context.Posts.Where(P => P.Id == createReplyPostDto.Postid).Select(P => P.UserId).FirstOrDefaultAsync();
        _logger.LogInformation(
    "Sending comment notification. PostId: {PostId}, SenderId: {SenderId}, ReceiverId: {ReceiverId}",
    createReplyPostDto.Postid,
    createReplyPostDto.UserId,
    z);
        await _servicebusPublisher.SendAsync(new NotificationDto
        {
            Content = "Commented under Your Post",
            Senderid = createReplyPostDto.UserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            Type = "Comment",
            ReceiverId = z
        });
        if (createReplyPostDto.Images.Count > 0)
        {
            var images = new List<Images>();
            foreach (var image in createReplyPostDto.Images)
            {
                var filename = Guid.NewGuid() + Path.GetExtension(image.FileName);
                images.Add(new Images
                {
                    //postid = createReplyPostDto.Postid,
                    ImageURL = filename,
                    ReplyId = reply.Id
                });
                var pathcombine = Path.Combine("wwwroot", "Uploads", filename);
                var filestream = new FileStream(pathcombine, FileMode.Create);
                await image.CopyToAsync(filestream);
            }
            _context.Images.AddRange(images);
            await _context.SaveChangesAsync();
        }
        if (res > 0)
        {
            return true;
        }
        return false;
            
        }catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Failed to create reply notification for post {PostId} by user {UserId}",
        createReplyPostDto.Postid,
        createReplyPostDto.UserId);

    return false;
}
        // Placeholder return value
    }
}