using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessageController:ControllerBase
{

    private readonly IMessageService _messageService;
    public MessageController(IMessageService messageService)
    {
        _messageService=messageService;
        
    }

    [HttpPost("{receiverid}/{content}")]
    public async Task<IActionResult> CreateMsgAsync(int receiverid,string content)
    {
        try
        {
            var response=await _messageService.CreateMessageAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value),content,receiverid);
            if (response!=null)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest("Something gone wrong");
            }
        }
        catch
        {
            return BadRequest("Something gone wrong");
        }
        
    }

    [HttpGet("{receiverid}")]
    public async Task<IActionResult> GetMessages(int receiverid,int? lastmessageid,int numberofmessages=30)
    {
        try
        {
            var res=await _messageService.GetMessagesBetweenUsersAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value),receiverid,lastmessageid,numberofmessages);
            if (res.Count() >= 0)
            {
                return Ok(res);

            }
            else
            {
                return BadRequest();
            }
        }
        catch
        {
            return BadRequest();
        }
    }
    
    [HttpGet("chats")]
    public async Task<IActionResult> GetAllPrevChatsForAUserAsync()
    {
        try
        {
            var result=await _messageService.GetAllPrevChatsForAUserAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value));
            if (result.Count() > 0)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch
        {
            return BadRequest();
        }
        
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var response=await _messageService.GetAllUsers();
            if (response.Count() > 0)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch
        {
            return BadRequest("Somethin Gone Wrong");
        }
    }

    [HttpPost("updateAll/{receiverid}")]
    public async Task<IActionResult> UpdateStatus(int receiverid)
    {
        try
        {
            var response =await _messageService.UpdateStatusAllAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value),receiverid);
            if (response.Count() > 0)
            {
                return Ok(response);
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            return BadRequest("Something gone wrong");
        }
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadMessages()
    {
        try
        {
            var count = await _messageService.GetUnreadMessagesAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value));
            return Ok(count);
        }
        catch
        {
            return BadRequest(0);
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetSearchResults(string searchTerm)
    {
        try
        {
            Console.WriteLine("search term in controller:", searchTerm);
            var results = await _messageService.GetSearchResultsAsync(searchTerm, int.Parse(HttpContext.User.FindFirst("userId")!.Value));
            return Ok(results);
        }
        catch
        {
            return BadRequest(0);
        }
    }

}