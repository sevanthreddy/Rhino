using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Backend.Services;

public class ServicebusReceiver : BackgroundService
{
    private readonly ServiceBusProcessor _processor;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServicebusReceiver> _logger;

    public ServicebusReceiver(
        ServiceBusClient client,
        IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<ServicebusReceiver> logger)
    {
        var queueName = configuration["ServiceBus:NotificationQueue"];

        _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false
        });
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("Service Bus receiver starting...");

    _processor.ProcessMessageAsync += ProcessMessageAsync;
    _processor.ProcessErrorAsync += ProcessErrorAsync;

    await _processor.StartProcessingAsync(stoppingToken);

    _logger.LogInformation("Service Bus processor started.");

    await Task.Delay(Timeout.Infinite, stoppingToken);
}

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var notification = JsonSerializer.Deserialize<NotificationDto>(body);

        if (notification == null)
        {
            _logger.LogWarning("Received invalid notification message");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var notificationService =
            scope.ServiceProvider
                  .GetRequiredService<INotificationService>();

        await notificationService.CreateNotificationAsync(notification);


        _logger.LogInformation("Received notification message: {MessageBody}", body);

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error");

        return Task.CompletedTask;
    }
}