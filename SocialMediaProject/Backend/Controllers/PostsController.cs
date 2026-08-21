using System;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    public readonly IPostService PostService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, ILogger<PostsController> logger)
    {
        PostService = postService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts()
    {
        _logger.LogInformation("GET POSTS STARTED");

        try
        {
            var posts = await PostService.GetPostsAsync(1);

            _logger.LogInformation("Posts loaded: {Count}", posts.Count());

            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET POSTS FAILED");

            return StatusCode(500, new
            {
                message = ex.Message,
                stackTrace = ex.StackTrace,
                inner = ex.InnerException?.ToString()
            });
        }
    }

    [HttpGet("post/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(int id)
    {
        var post = await PostService.GetPostByIdAsync(int.Parse(HttpContext.User.FindFirst("userId")!.Value), id);
        return post != null ? Ok(post) : NotFound();
    }


    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostDto createPostDto)
    {
        if (createPostDto == null || string.IsNullOrWhiteSpace(createPostDto.Content))
        {
            return BadRequest("Post content cannot be empty.");
        }
        createPostDto.UserId = int.Parse(HttpContext.User.FindFirst("userId")!.Value);

        var createdPost = await PostService.CreatePostAsync(createPostDto);
        return Ok(new
{
    id = createdPost.Id
});
    }

    [HttpPost("like/{postid}")]
    public async Task<IActionResult> LikePost(int postid)
    {
        if (postid == 0)
        {
            return BadRequest("postid is null");
        }

        var (Nooflikes, result) = await PostService.LikePostAsync(postid, int.Parse(HttpContext.User.FindFirst("userId")!.Value));
        return Ok(new
        {
            likeCount = Nooflikes,
            result = result
        });
    }



}