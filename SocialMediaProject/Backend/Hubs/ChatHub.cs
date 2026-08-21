using System.Globalization;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IPresenceService _presenceService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IMessageService messageService, IPresenceService presenceService, ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _presenceService = presenceService;
        _logger = logger;
    }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        await _presenceService.AddUserConnection(int.Parse(userId), Context.ConnectionId);

        _logger.LogInformation("User {UserId} connected", userId);
        var x = await _presenceService.GetAllUsersOnline();
        await Clients.Caller.SendAsync("AllUsers", x);
        await Clients.All.SendAsync("UserOnline", userId);

        await base.OnConnectedAsync();
    }
    public async Task SendMessage(int receiverid, string message)
    {
        _logger.LogInformation("Sending chat message to user {ReceiverId}: {Message}", receiverid, message);

        var x = await _messageService.CreateMessageAsync(int.Parse(Context.User?.FindFirst("userId")?.Value), message, receiverid);
        if (x != null)
        {
            await Clients.User(receiverid.ToString())
        .SendAsync("ReceiveMessage", x);
        await Clients.User(Context.User?.FindFirst("userId")?.Value.ToString()).SendAsync("ReceiveMessage", x);
        }

    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        // Called automatically when the connection closes
        await _presenceService.RemoveUserConnection(int.Parse(userId), Context.ConnectionId);
        _logger.LogInformation("User {UserId} removed from online users", userId);
        await Clients.All.SendAsync("UserOffline", userId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendTypingMessage(int receiverid)
    {
        _logger.LogInformation("SendTypingMessage started for receiver {ReceiverId}", receiverid);
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Clients.User(receiverid.ToString()).SendAsync("TypingMessage", userId);
        _logger.LogInformation("SendTypingMessage completed for receiver {ReceiverId}", receiverid);


    }

    public async Task SendStopTypingMessage(int receiverid)
    {
        _logger.LogInformation("SendStopTypingMessage started for receiver {ReceiverId}", receiverid);
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Clients.User(receiverid.ToString()).SendAsync("StopTypingMessage", userId);
        _logger.LogInformation("SendStopTypingMessage completed for receiver {ReceiverId}", receiverid);


    }

    public async Task RegisterDeliveredMesssage(int messageId,int id)//id represents the person whose message has been delivered
    {
        _logger.LogInformation("Registering delivered message {MessageId} for user {UserId}", messageId, id);
        var x = await _messageService.UpdateStatusAsync(messageId, "Delivered");//updating db 
        if (x)
        {
            await Clients.User(id.ToString()).SendAsync("DeliveredMessage",messageId);//sending the delivered status of msg to  id
        }
        _logger.LogInformation("RegisterDeliveredMessage completed for message {MessageId}", messageId);
    }

    public async Task RegisterReadMesssage(List<int> messageIds, int id)//id represents the person whose message has been read
    {
        _logger.LogInformation("Registering {MessageCount} read messages for user {UserId}", messageIds.Count, id);
        foreach (var msgId in messageIds)
        {
            await Clients.User(id.ToString()).SendAsync("ReadMessage", msgId);//sending the read status of msg to  id
        }
        _logger.LogInformation("RegisterReadMessage completed for user {UserId}", id); 
    }
}