using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using shared.Contract;
using shared.RabbitMq;

namespace OrderService.Services;

public class InventoryFailedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<InventoryFailedConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public InventoryFailedConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<InventoryFailedConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Pass,
            VirtualHost = _options.VHost
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: RabbitMqConstants.InventoryExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqConstants.OrdersInventoryFailedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: RabbitMqConstants.OrdersInventoryFailedQueue,
            exchange: RabbitMqConstants.InventoryExchange,
            routingKey: RabbitMqConstants.InventoryReservationFailedRoutingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: RabbitMqConstants.OrdersInventoryFailedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("InventoryFailedConsumerService listening on {Queue}",
            RabbitMqConstants.OrdersInventoryFailedQueue);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var evt = JsonSerializer.Deserialize<InventoryReservationFailedEvent>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null)
                throw new InvalidOperationException("Payload deserialized to null");

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IInventoryFailedHandler>();
            await handler.HandleAsync(messageId, evt, CancellationToken.None);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process compensation {MessageId}; requeueing", messageId);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
