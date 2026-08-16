using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;

    public NotificationHub(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task SendNotification(
        int receiverId,
        NotificationDto notification)
    {
        await Clients.User(receiverId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }
}