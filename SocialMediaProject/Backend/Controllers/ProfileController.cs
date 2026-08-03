using System;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    public ProfileController(IProfileService profileService)
    {
        _profileService=profileService;
    }

    [HttpPost("Save")]
    public async Task<IActionResult> SaveProfileInfoAsync([FromForm] EditProfileDto editProfileDto)
    {
        try
        {
            editProfileDto.userid=int.Parse(HttpContext.User.FindFirst("userId")!.Value);
            var result=await _profileService.SaveProfileInfoAsync(editProfileDto);
            if (result)
            {
                return Ok("Profile Saved");

            }
            else
            {
                return BadRequest("Profile Save was not Successfull");
            }
        }
        catch
        {
            return BadRequest("Profile Save was not Successfull");
            
        }
        
    }
 

}