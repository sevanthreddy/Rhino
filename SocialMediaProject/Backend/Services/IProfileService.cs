public interface IProfileService
{
    Task<bool> SaveProfileInfoAsync(EditProfileDto editProfileDto);
}