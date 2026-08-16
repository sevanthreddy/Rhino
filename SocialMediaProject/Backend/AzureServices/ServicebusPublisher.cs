using Azure.Messaging.ServiceBus;
using System.Text.Json;

public class ServiceBusPublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(
        ServiceBusClient client,
        IConfiguration configuration)
    {
        var queueName = configuration["ServiceBus:NotificationQueue"]!;

        _sender = client.CreateSender(queueName);
    }

    public async Task SendAsync(object eventData)
    {
        var json = JsonSerializer.Serialize(eventData);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(message);
    }
}