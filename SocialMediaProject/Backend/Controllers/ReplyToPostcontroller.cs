using System.Reflection.Metadata.Ecma335;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReplyTocontroller : ControllerBase
{
    public readonly IReplyService _replyService;

    public ReplyTocontroller(IReplyService replyService)
    {
        _replyService=replyService;
    }

    [HttpGet("Replies/{id}")]
    public async Task<IActionResult> GetReplyPostDtosAsync(int id)
    {
        var result = await _replyService.GetReplyPostDtosAsync(id);
        return result!=null?Ok(result):NotFound();
    }

    [HttpPost("post")]
    public async Task<IActionResult> CreateReplyAsync([FromForm] CreateReplyPostDto createReplyPostDto)
    {
        createReplyPostDto.UserId=int.Parse(HttpContext.User.FindFirst("userId")!.Value);
        var result=await _replyService.CreateReplyAsync(createReplyPostDto);
        if (result == true)
        {
            return Ok("Reply Created");
        }
        return BadRequest("Reply failed");
    }
}