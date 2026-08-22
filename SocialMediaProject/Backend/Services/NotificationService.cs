using Backend.Data;
using Backend.Hubs;
using Backend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, IHubContext<ChatHub> hubContext, ILogger<NotificationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<bool> CreateNotificationAsync(NotificationDto notification)
    {
        try
        {
            _logger.LogInformation("CreateNotificationAsync method started");
            var x = new Notifications
            {
                Content = notification.Content,
                Senderid = notification.Senderid,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                Type = notification.Type,
                ReceiverId = notification.ReceiverId
            };

            await _context.Notifications.AddAsync(x);
            await _context.SaveChangesAsync();
            if (x.Senderid != x.ReceiverId)
            {
                await _hubContext.Clients.User(notification.ReceiverId.ToString()).SendAsync("ReceiveNotification", notification);
            }
             _logger.LogInformation("CreateNotificationAsync method ended");
             return true;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "CREATE NOTIFICATION FAILED for receiver {ReceiverId}",
                notification.ReceiverId);
            return false;

            
        }
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId)
    {
        try
        {
            var res = await _context.Notifications.Where(n => n.ReceiverId == userId && n.Senderid != userId).Include(n => n.Sender).OrderByDescending(n => n.CreatedAt).ToListAsync();

            var datatofrontend = res.Select(x => new NotificationDto
            {
                Id = x.Id,
                Content = x.Content,
                Senderid = x.Senderid,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Type = x.Type,
                SenderUserName = x.Sender.Username

            });
            return datatofrontend;

        }
        catch (Exception e)
        {
            _logger.LogError(e, "GET NOTIFICATIONS FAILED for user {UserId}", userId);
            return Enumerable.Empty<NotificationDto>();
        }
    }

    public async Task<bool> MarkNotificationAsReadAsync(int userid)
    {
        try
        {
            var allnotificationsofuser = await _context.Notifications.Where(n => n.ReceiverId == userid).ToListAsync();
            for (int i = 0; i < allnotificationsofuser.Count(); i = i + 1)
            {
                allnotificationsofuser[i].IsRead = true;
            }
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error Occured in MarkNotificationAsReadAsync");
            return false;
        }
    }


}