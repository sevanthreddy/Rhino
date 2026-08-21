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
    public PostsController(IPostService postService)
    {
        PostService = postService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts()
    {
        Console.WriteLine("========== GET /api/Posts ==========");

        try
        {
            Console.WriteLine("Calling PostService...");

            var posts = await PostService.GetPostsAsync(1);

            Console.WriteLine("PostService returned successfully");

            return Ok(posts);
        }
        catch (Exception ex)
        {
            Console.WriteLine("========== POSTS ERROR ==========");
            Console.WriteLine(ex.ToString());

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
        return CreatedAtAction(nameof(GetPosts), new { id = createdPost.Id }, createdPost);
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