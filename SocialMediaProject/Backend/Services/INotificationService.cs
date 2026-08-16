public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId);
    Task MarkNotificationAsReadAsync(int notificationId);
    Task CreateNotificationAsync(NotificationDto notification);
}