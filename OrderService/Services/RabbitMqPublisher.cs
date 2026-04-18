using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using shared.RabbitMq;
using System.Text;

namespace OrderService.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
        private readonly RabbitMqOptions _options;
        private IConnection? _connection;
        private IChannel? _channel;


        public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;


         }


    public async Task ConnectAsync()
    {
        if (_connection is not null && _channel is not null)
            return;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Pass,
            VirtualHost = _options.VHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
    }
    public async Task PublishAsync(string routingKey, string messageType, byte[] body, int messageId, CancellationToken ct = default)
    {
        if (_channel is null)
            await ConnectAsync();

        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel could not be created.");
        
        await _channel.ExchangeDeclareAsync(
            exchange: RabbitMqConstants.OrdersExchange,
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
            exchange: RabbitMqConstants.OrdersExchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);



    }
    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    




}
