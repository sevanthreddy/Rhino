using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Backend.Services;

public class ServicebusReceiver : BackgroundService
{
    private readonly ServiceBusProcessor _processor;

    private readonly IServiceScopeFactory _scopeFactory;

    public ServicebusReceiver(
        ServiceBusClient client,
        IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        var queueName = configuration["ServiceBus:NotificationQueue"];

        _processor = client.CreateProcessor(queueName);
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var notification = JsonSerializer.Deserialize<NotificationDto>(body);

        if (notification == null)
        {
            Console.WriteLine("Invalid notification message");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var notificationService =
            scope.ServiceProvider
                  .GetRequiredService<INotificationService>();

        await notificationService.CreateNotificationAsync(notification);


        Console.WriteLine($"Received message: {body}");

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Service Bus error: {args.Exception}");

        return Task.CompletedTask;
    }
}