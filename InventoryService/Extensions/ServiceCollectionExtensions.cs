using InventoryService.Data;
using InventoryService.Repositories;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using shared.RabbitMq;

namespace InventoryService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryServices(
        this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitMqOptions>(config.GetSection("RabbitMq"));
        services.Configure<OutboxOptions>(config.GetSection("Outbox"));

        services.AddDbContext<InventoryDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IOrderCreatedHandler, OrderCreatedHandler>();

        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        services.AddHostedService<OrderCreatedConsumerService>();
        services.AddHostedService<OutboxPublisherService>();

        return services;
    }
}
