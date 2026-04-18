using InventoryService.Repositories;
using Microsoft.Extensions.Options;
using shared.RabbitMq;

namespace InventoryService.Services;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqPublisher _publisher;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IRabbitMqPublisher publisher,
        IOptions<OutboxOptions> options,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "InventoryService OutboxPublisherService started PollIntervalSeconds={Poll} BatchSize={Batch}",
            _options.PollIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error processing inventory outbox batch"); }

            try { await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken); }
            catch (TaskCanceledException) { }
        }
    }

    private async Task ProcessBatchAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var messages = await outbox.GetUnprocessedBatchAsync(_options.BatchSize);
        if (messages.Count == 0) return;

        _logger.LogInformation("Publishing {Count} inventory outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var (exchange, routingKey) = RouteFor(message.EventType);
                await _publisher.PublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    messageType: message.EventType,
                    body: System.Text.Encoding.UTF8.GetBytes(message.Payload),
                    messageId: message.Id);
                await outbox.MarkProcessedAsync(message.Id);
                _logger.LogInformation("Published outbox message Id={Id} Type={Type}", message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message Id={Id}", message.Id);
                await outbox.MarkFailedAsync(message.Id, ex.Message);
            }
        }
    }

    private static (string exchange, string routingKey) RouteFor(string eventType) =>
        eventType switch
        {
            nameof(shared.Contract.InventoryReservationFailedEvent) =>
                (RabbitMqConstants.InventoryExchange, RabbitMqConstants.InventoryReservationFailedRoutingKey),
            _ => throw new InvalidOperationException($"No route configured for event type '{eventType}'")
        };
}

public class OutboxOptions
{
    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 20;
}
