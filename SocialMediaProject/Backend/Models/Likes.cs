using System.ComponentModel.DataAnnotations;
using Backend.Models;

public class Likes
{
    [Key]
    public int Id { get; set; }

    public int? Postid { get; set; }

    public int Userid { get; set; }

    public DateTime LikedAt { get; set; }

    public User User { get; set; } = null!;

    public Post Post { get; set; } = null!;

    public int? RepliesId { get; set; }

    public Replies Reply {get;set;}

}