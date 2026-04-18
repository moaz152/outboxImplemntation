

namespace shared.RabbitMq;

public static class RabbitMqConstants
{
    public const string OrdersExchange = "orders.exchange";
    public const string OrderCreatedRoutingKey = "order.created";
    public const string InventoryOrderCreatedQueue = "inventory.order-created";

    public const string OrderCreatedMessageType = nameof(shared.Contract.OrderCreatedEvent);
}
