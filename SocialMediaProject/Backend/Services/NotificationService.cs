using Backend.Data;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context,IHubContext<ChatHub> hubContext,ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext=hubContext;
        _logger=logger;
    }

    public async Task CreateNotificationAsync(NotificationDto notification)
    {
        try
        {
            var x=new Notifications
            {
                Content=notification.Content,
                Senderid=notification.Senderid,
                IsRead=notification.IsRead,
                CreatedAt=notification.CreatedAt,
                Type=notification.Type,
                ReceiverId=notification.ReceiverId
            };

            await _context.Notifications.AddAsync(x);
            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(notification.ReceiverId.ToString()).SendAsync("ReceiveNotification",notification);
        }
        catch(Exception e)
        {
            _logger.LogError(e, "CREATE NOTIFICATION FAILED for receiver {ReceiverId}", notification.ReceiverId);
            return;
            
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId)
    {
        try
        {
            var res=await _context.Notifications.Where(n=>n.ReceiverId==userId).Include(n=>n.Sender).OrderByDescending(n=>n.CreatedAt).ToListAsync();

            var datatofrontend=res.Select(x=>new NotificationDto
            {
                Id=x.Id,
                Content=x.Content,
                Senderid=x.Senderid,
                IsRead=x.IsRead,
                CreatedAt=x.CreatedAt,
                Type=x.Type,
                SenderUserName=x.Sender.Username
                
            });
            return datatofrontend;

        }
        catch(Exception e)
        {
            _logger.LogError(e, "GET NOTIFICATIONS FAILED for user {UserId}", userId);
            return Enumerable.Empty<NotificationDto>();
        }
    }

    public Task MarkNotificationAsReadAsync(int notificationId)
    {
        throw new NotImplementedException();
    }

   
}