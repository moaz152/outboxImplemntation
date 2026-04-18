namespace OrderService.Services
{
    public interface IRabbitMqPublisher : IAsyncDisposable
    {
        Task PublishAsync(string routingKey, string messageType, byte[] body, int messageId, CancellationToken ct = default);
    }
}
