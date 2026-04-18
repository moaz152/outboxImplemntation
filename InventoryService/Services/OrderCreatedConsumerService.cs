using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using shared.Contract;
using shared.RabbitMq;

namespace InventoryService.Services;

public class OrderCreatedConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OrderCreatedConsumerService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public OrderCreatedConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<OrderCreatedConsumerService> logger)
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
            exchange: RabbitMqConstants.OrdersExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: RabbitMqConstants.InventoryOrderCreatedQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: RabbitMqConstants.InventoryOrderCreatedQueue,
            exchange: RabbitMqConstants.OrdersExchange,
            routingKey: RabbitMqConstants.OrderCreatedRoutingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: RabbitMqConstants.InventoryOrderCreatedQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("OrderCreatedConsumerService started; listening on {Queue}",
            RabbitMqConstants.InventoryOrderCreatedQueue);

        // Keep the service alive until shutdown.
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
        var body = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (evt is null)
                throw new InvalidOperationException("Payload deserialized to null");

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IOrderCreatedHandler>();
            await handler.HandleAsync(messageId, evt, CancellationToken.None);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}; requeueing", messageId);
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
