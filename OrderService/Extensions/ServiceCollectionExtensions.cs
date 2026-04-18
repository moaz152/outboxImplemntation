using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.repositories;
using OrderService.Services;
using shared.RabbitMq;

namespace OrderService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));
        services.Configure<OutboxPublisherService.OutboxOptions>(config.GetSection("Outbox"));

        services.AddDbContext<OrderDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

        services.AddScoped<IOrderService, Services.OrderService>();
        services.AddScoped<IInventoryFailedHandler, InventoryFailedHandler>();

        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        services.AddHostedService<OutboxPublisherService>();
        services.AddHostedService<InventoryFailedConsumerService>();

        return services;
    }
}
