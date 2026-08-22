using Azure.Messaging.ServiceBus;
using System.Text.Json;

public class ServiceBusPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(
        ServiceBusClient client,
        IConfiguration configuration,
        ILogger<ServiceBusPublisher> logger)
    {
        var queueName = configuration["ServiceBus:NotificationQueue"]!;

        _sender = client.CreateSender(queueName);
        _logger = logger;
    }

    public async Task SendAsync(object eventData)
    {
        try
        {
            _logger.LogInformation("sending notification to service bus method statrted");
            var json = JsonSerializer.Serialize(eventData);

            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json"
            };

            await _sender.SendMessageAsync(message);
            _logger.LogInformation(
                "Published Service Bus event of type {EventType} with payload size {PayloadSize} bytes",
                eventData.GetType().Name,
                json.Length);
                _logger.LogInformation("sending notification to service bus method ended");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Service Bus event of type {EventType}", eventData.GetType().Name);
            throw;
        }
    }
}