using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController:ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService=notificationService;
        _logger=logger;
    }

    [HttpGet("getall")]
    public async  Task<IActionResult> GetAllNotifications()
    {
        var userId = int.Parse(HttpContext.User.FindFirst("userId")!.Value);

        try
        {
            var res=await _notificationService.GetNotificationsForUserAsync(userId);
            _logger.LogInformation("Retrieved notifications for user {UserId}", userId);
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve notifications for user {UserId}", userId);
            return BadRequest("Something gone wrong");
        }
    }

    [HttpPut]
    public async Task<IActionResult> MarkNotificationsASRead()
    {
        var userId = int.Parse(HttpContext.User.FindFirst("userId")!.Value);

        try
        {
            var res = await _notificationService.MarkNotificationAsReadAsync(userId);
            if (res)
            {
                _logger.LogInformation("Marked notifications as read for user {UserId}", userId);
                return Ok("Marking notifications as Read");
            }

            _logger.LogWarning("Failed to mark notifications as read for user {UserId}", userId);
            return BadRequest("Something gone wrong");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notifications as read for user {UserId}", userId);
            return BadRequest("Something gone wrong");
        }
    }
    
}