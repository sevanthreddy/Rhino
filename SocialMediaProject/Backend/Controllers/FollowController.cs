using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowController:ControllerBase
{

    private readonly IFollowService _followService;

    public FollowController(IFollowService followService)
    {
        _followService=followService;
    }

    
    [HttpPost("{username}")]
    public async Task<IActionResult> Follow(string username)
    {
        try
        {
            var result=await _followService.FollowUserAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value),username);
            if (result == true)
            {
                return Ok("Follow Operation successfull");
            }
            else
            {
                return BadRequest("Follow user was unsuccesfull");
            }
        }
        catch
        {
            return BadRequest("Error occured");
        }
        
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetFollowInfoResult(string username)
    {
        try
        {
            var result=await _followService.GetFollowInfoAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value),username);
            if (result!=null)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest("You dont follow him");
            }
        }
        catch
        {
            return BadRequest("Op was unsuccessfull");
        }
    }
    
    [HttpGet("{username}/{tab}")]
    public async Task<IActionResult> GetClickedTabData(string username,string tab)
    {
        try
        {
            var result=await _followService.GetClickedTabDataAsync(username,tab);
            if (result.Count() > 0)
            {
                return Ok(result);
            }
            else
            {
                return Ok("No Posts");
            }
        }
        catch
        {
            return BadRequest("Something gone wrong");
        }
        
    }
    

}