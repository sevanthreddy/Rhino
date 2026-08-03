using Backend.Data;
using Microsoft.EntityFrameworkCore;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    public ProfileService(ApplicationDbContext context)
    {
        _context=context;
    }
    public async Task<bool> SaveProfileInfoAsync(EditProfileDto editProfileDto)
    {
        try
        {
            var user=await _context.Users.Where(u=>u.Id==editProfileDto.userid).FirstOrDefaultAsync();
            user.Name=editProfileDto.Name;
            user.About=editProfileDto.Bio;
            if (editProfileDto.profileimage!=null)
            {
                var filename=Guid.NewGuid()+Path.GetExtension(editProfileDto.profileimage.FileName);
                var pathcombine = Path.Combine("wwwroot", "Uploads", filename);
                var filestream = new FileStream(pathcombine, FileMode.Create);
                await editProfileDto.profileimage.CopyToAsync(filestream);
                user.ProfileImageURL=filename;
                
            }
            await _context.SaveChangesAsync();
            return true;

        }
        catch
        {
            return false;
        }
    }
}