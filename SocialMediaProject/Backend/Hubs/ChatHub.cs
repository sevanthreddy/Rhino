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

    public ChatHub(IMessageService messageService, IPresenceService presenceService)
    {
        _messageService = messageService;
        _presenceService = presenceService;
    }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        await _presenceService.AddUserConnection(int.Parse(userId), Context.ConnectionId);

        Console.WriteLine($"User {userId} connected.");
        var x = await _presenceService.GetAllUsersOnline();
        await Clients.Caller.SendAsync("AllUsers", x);
        await Clients.All.SendAsync("UserOnline", userId);

        await base.OnConnectedAsync();
    }
    public async Task SendMessage(int receiverid, string message)
    {
        Console.WriteLine(message);

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
        Console.WriteLine("user removed from onlineusers: " + userId);
        await Clients.All.SendAsync("UserOffline", userId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendTypingMessage(int receiverid)
    {
        Console.WriteLine("SendTyping Message method  started");
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Clients.User(receiverid.ToString()).SendAsync("TypingMessage", userId);
        Console.WriteLine("SendTyping Message method done");


    }

    public async Task SendStopTypingMessage(int receiverid)
    {
        Console.WriteLine("SendStopTyping Message started");
        var userId = Context.User?.FindFirst("userId")?.Value;
        await Clients.User(receiverid.ToString()).SendAsync("StopTypingMessage", userId);
        Console.WriteLine("SendStopTyping Message method done");


    }

    public async Task RegisterDeliveredMesssage(int messageId,int id)//id represents the person whose message has been delivered
    {
        Console.WriteLine(messageId);
        var x = await _messageService.UpdateStatusAsync(messageId, "Delivered");//updating db 
        if (x)
        {
            await Clients.User(id.ToString()).SendAsync("DeliveredMessage",messageId);//sending the delivered status of msg to  id
        }
        Console.WriteLine("RegisterDeliveredMesssage method completed");
    }

    public async Task RegisterReadMesssage(List<int> messageIds, int id)//id represents the person whose message has been read
    {
        Console.WriteLine(messageIds);
        Console.WriteLine(
    $"Context user: {Context.User?.FindFirst("userId")?.Value}");
        foreach (var msgId in messageIds)
        {
            await Clients.User(id.ToString()).SendAsync("ReadMessage", msgId);//sending the read status of msg to  id
        }
        Console.WriteLine("RegisterReadMesssage method completed"); 
    }
}