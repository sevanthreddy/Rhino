public interface IPostService
{
    Task<IEnumerable<PostDto>> GetPostsAsync(int userid);

        Task<PostDto> GetPostByIdAsync(int userid,int postid);


    Task<Post> CreatePostAsync(CreatePostDto createPostDto);

    Task<(int,bool)> LikePostAsync(int postid,int userid);
   
}