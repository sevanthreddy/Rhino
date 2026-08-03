using Backend.Models;

public class Follow
{
    public int id { get; set; }

    public int FollowerId { get; set; }
    public User Follower { get; set; }

    public int FollowingId { get; set; }
    public User Following { get; set; }

    public DateTime CreatedAt { get; set; }


}