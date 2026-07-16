namespace Backend.Services;

public interface IReplyService
{
    Task<bool> CreateReplyAsync(CreateReplyPostDto createReplyPostDto);

    Task<IEnumerable<ReplyPostDto>>  GetReplyPostDtosAsync(int postid);
}