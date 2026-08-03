public class EditProfileDto
{
    public int userid{get;set;}
    public string? Name { get; set; }

    public string? Bio { get; set; }
    public IFormFile profileimage { get; set; }

}