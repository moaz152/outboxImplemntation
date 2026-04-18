namespace InventoryService.Services;

public interface IRabbitMqPublisher : IAsyncDisposable
{
    Task PublishAsync(
        string exchange,
        string routingKey,
        string messageType,
        byte[] body,
        int messageId,
        CancellationToken ct = default);
}
