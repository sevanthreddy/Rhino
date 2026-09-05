using Backend.Data;
using Microsoft.EntityFrameworkCore;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly BlobStorageService _blobStorage;

    public ProfileService(ApplicationDbContext context, BlobStorageService blobStorage)
    {
        _context=context;
        _blobStorage=blobStorage;
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
                var filename =
                    Guid.NewGuid() + Path.GetExtension(editProfileDto.profileimage.FileName);

                var imageUrl =
                    await _blobStorage.UploadAsync(editProfileDto.profileimage, filename);

                
                
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