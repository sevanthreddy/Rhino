using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController:ControllerBase
{
    public readonly INotificationService _notificationService;
    public NotificationsController(INotificationService notificationService)
    {
        _notificationService=notificationService;
    }

    [HttpGet("getall")]
    public async  Task<IActionResult> GetAllNotifications()
    {
        var res=await _notificationService.GetNotificationsForUserAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value));
        return res!=null?Ok(res):BadRequest();
    }
    
}