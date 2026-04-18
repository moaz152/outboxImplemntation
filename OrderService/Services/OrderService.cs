using OrderService.Data;
using OrderService.repositories;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _db;
        private readonly IOrderRepository _orders;
        private readonly IOutboxRepository _outbox;

        public OrderService(OrderDbContext db, IOrderRepository orders, IOutboxRepository outbox)
        {
            _db = db;
            _orders = orders;
            _outbox = outbox;
        }


        public async Task CreateOrderAsync(int productId, int quantity)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {

                var order = new Entities.Order
                {
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                };
                await _orders.AddOrderAsync(order);
                var outboxMessage = new Entities.OutboxMessage
                {
                    EventType = "OrderCreated",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new { order.Id, order.ProductId, order.Quantity }),
                    CreatedAt = DateTime.UtcNow
                };
                await _outbox.AddOutboxMessageAsync(outboxMessage);
                await _db.SaveChangesAsync();


                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
