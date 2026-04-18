namespace shared.RabbitMq;

public static class RabbitMqConstants
{
    public const string OrdersExchange = "orders.exchange";
    public const string OrderCreatedRoutingKey = "order.created";
    public const string InventoryOrderCreatedQueue = "inventory.order-created";
    public const string OrderCreatedMessageType = nameof(shared.Contract.OrderCreatedEvent);

    public const string InventoryExchange = "inventory.exchange";
    public const string InventoryReservationFailedRoutingKey = "inventory.reservation.failed";
    public const string OrdersInventoryFailedQueue = "orders.inventory-failed";
    public const string InventoryReservationFailedMessageType = nameof(shared.Contract.InventoryReservationFailedEvent);
}
