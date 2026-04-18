
using Microsoft.Extensions.Options;
using OrderService.repositories;

namespace OrderService.Services;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqPublisher _publisher;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;
    public OutboxPublisherService(IServiceScopeFactory scopeFactory, IRabbitMqPublisher publisher, IOptions<OutboxOptions> options, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // fetch db vlues, publish to rabbitmq, mark as published in db, repeat every _options.PollIntervalSeconds seconds
        _logger.LogInformation("OutboxPublisherService started with PollIntervalSeconds={PollIntervalSeconds} and BatchSize={BatchSize}", _options.PollIntervalSeconds, _options.BatchSize);
        while (stoppingToken.IsCancellationRequested)
        {
            try 
            {
                await ProcessBatchAsync();
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox batch");


            }
            try { await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken); } catch (TaskCanceledException) { }


        }
}

    private async Task ProcessBatchAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var outBox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var messages = await outBox.GetUnprocessedBatchAsync(_options.BatchSize);

        _logger.LogInformation("Publishing {Count} outbox messages", messages.Count);

        foreach (var message in messages) 
        {
            try 
            {
                await _publisher.PublishAsync(
                    routingKey: "order.created",
                    messageType: message.EventType,
                    body: System.Text.Encoding.UTF8.GetBytes(message.Payload),
                    messageId: message.Id,
                    ct: CancellationToken.None);
                await outBox.MarkProcessedAsync(message.Id);
                _logger.LogInformation("Successfully published outbox message with Id={MessageId}", message.Id);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Failed to publish outbox message with Id={MessageId}", message.Id);
                await outBox.MarkFailedAsync(message.Id, ex.Message);
            }

        }
    }

    public class OutboxOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
}
}
