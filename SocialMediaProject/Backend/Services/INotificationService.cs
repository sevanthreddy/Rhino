public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsForUserAsync(int userId);
    Task<bool> MarkNotificationAsReadAsync(int userid);
    Task<bool> CreateNotificationAsync(NotificationDto notification);
}