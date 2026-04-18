using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using shared.RabbitMq;

namespace InventoryService.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options) => _options = options.Value;

    private async Task ConnectAsync(CancellationToken ct)
    {
        if (_connection is not null && _channel is not null) return;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Pass,
            VirtualHost = _options.VHost
        };

        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
    }

    public async Task PublishAsync(
        string exchange,
        string routingKey,
        string messageType,
        byte[] body,
        int messageId,
        CancellationToken ct = default)
    {
        await ConnectAsync(ct);
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel could not be created.");

        await _channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId.ToString(),
            Type = messageType,
            ContentType = "application/json",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
