using Backend.Data;
using Backend.Migrations;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    public MessageService(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }
    public async Task<MessageDto> CreateMessageAsync(int senderid, string content, int receiverid)
    {
        try
        {
            var r = await _context.Message.AddAsync(new Message
            {
                Content = content,
                SenderId = senderid,
                ReceiverId = receiverid,
                SentAt = DateTime.UtcNow,
                Status = "Sent"
            });
            await _context.SaveChangesAsync();
            return new MessageDto
            {
                MessageId = r.Entity.MessageId,
                SenderId = r.Entity.SenderId,
                ReceiverId = r.Entity.ReceiverId,
                Content = r.Entity.Content,
                SentAt = r.Entity.SentAt,
                Status = r.Entity.Status
            };
        }
        catch
        {
            return new MessageDto();
        }
    }

    public async Task<IEnumerable<ChatInfoDto>> GetAllPrevChatsForAUserAsync(int mainid)
    {
        try
        {
            var people = (await _context.Message
    .Where(m => m.SenderId == mainid || m.ReceiverId == mainid)
    .Select(m => m.SenderId == mainid
        ? new ChatInfoDto
        {
            userid = m.ReceiverId,
            Name = m.Receiver.Username,
            status = m.Status
        }
        : new ChatInfoDto
        {
            userid = m.SenderId,
            Name = m.Sender.Username,
            status = m.Status
        })
    .ToListAsync())
    .GroupBy(x => x.userid)
    .Select(g => g.First())
    .ToList();
            foreach (var p in people)
            {
                Console.WriteLine($"{p.userid} {p.Name} {p.status}");
            }
            return people;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Enumerable.Empty<ChatInfoDto>();

        }
    }

    public async Task<IEnumerable<ChatInfoDto>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users.Select(x => new ChatInfoDto
            {
                Name = x.Username,
                userid = x.Id
            }).ToListAsync();
            return users;
        }
        catch
        {
            return Enumerable.Empty<ChatInfoDto>();
        }
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesBetweenUsersAsync(int senderid, int receiverid, int? lastmessageid, int numberofmessages = 30)
    {
        try
        {
            Console.WriteLine($"GetMessagesBetweenUsersAsync called with senderid: {senderid}, receiverid: {receiverid}, lastmessageid: {lastmessageid}, numberofmessages: {numberofmessages}");
            var query = _context.Message.Where(m =>
    (m.SenderId == senderid && m.ReceiverId == receiverid) ||
    (m.SenderId == receiverid && m.ReceiverId == senderid));

            if (lastmessageid.HasValue)
            {
                query = query.Where(m => m.MessageId < lastmessageid.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.MessageId)
                .Take(numberofmessages)
                .ToListAsync();

            messages.Reverse();

            return messages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                SentAt = m.SentAt,
                Status = m.Status
            });
        }
        catch
        {
            return Enumerable.Empty<MessageDto>();
        }
    }

    public async Task<bool> UpdateStatusAsync(int messageid, string status)
    {
        try
        {
            var res = await _context.Message.Where(m => m.MessageId == messageid).OrderByDescending(m => m.SentAt)
    .FirstOrDefaultAsync();
            res.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

    }
    public async Task<IEnumerable<int>> UpdateStatusAllAsync(int senderid, int receiverid)
    {
        try
        {
            var messages = await _context.Message.Where(m => m.SenderId == receiverid && m.ReceiverId == senderid).ToListAsync();
            foreach (var message in messages)
            {
                message.Status = "Read";
            }
            await _context.SaveChangesAsync();
            return messages.Select(m => m.MessageId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Enumerable.Empty<int>();
        }

    }

    public async Task<int> GetUnreadMessagesAsync(int userid)
    {
        try
        {
            var count = await _context.Message
         .Where(m => m.ReceiverId == userid && m.Status != "Read")
         .Select(m => m.SenderId)
         .Distinct()
         .CountAsync();

            return count;
        }
        catch
        {
            return 0;
        }
    }

}