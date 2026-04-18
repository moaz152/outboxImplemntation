using System.Text.Json;
using InventoryService.Data;
using InventoryService.Entities;
using InventoryService.Repositories;
using shared.Contract;

namespace InventoryService.Services;

public class OrderCreatedHandler : IOrderCreatedHandler
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryRepository _inventory;
    private readonly IProcessedMessageRepository _processed;
    private readonly IOutboxRepository _outbox;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(
        InventoryDbContext db,
        IInventoryRepository inventory,
        IProcessedMessageRepository processed,
        IOutboxRepository outbox,
        ILogger<OrderCreatedHandler> logger)
    {
        _db = db;
        _inventory = inventory;
        _processed = processed;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, OrderCreatedEvent evt, CancellationToken ct)
    {
        if (await _processed.ExistsAsync(messageId))
        {
            _logger.LogInformation("Skipping already-processed message {MessageId}", messageId);
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var item = await _inventory.GetAsync(evt.ProductId);

            if (item is null || item.Quantity < evt.Quantity)
            {
                var available = item?.Quantity ?? 0;
                var reason = item is null
                    ? $"Unknown ProductId={evt.ProductId}"
                    : $"Insufficient stock: requested={evt.Quantity}, available={available}";

                _logger.LogWarning(
                    "Cannot reserve for OrderId={OrderId} ProductId={ProductId}: {Reason}",
                    evt.Id, evt.ProductId, reason);

                var failedEvent = new InventoryReservationFailedEvent
                {
                    OrderId = evt.Id,
                    ProductId = evt.ProductId,
                    RequestedQuantity = evt.Quantity,
                    AvailableQuantity = available,
                    Reason = reason
                };

                await _outbox.AddAsync(new OutboxMessage
                {
                    EventType = nameof(InventoryReservationFailedEvent),
                    Payload = JsonSerializer.Serialize(failedEvent),
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                item.Quantity -= evt.Quantity;
                item.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Reserved {Qty} of ProductId={ProductId} for OrderId={OrderId} (remaining={Remaining})",
                    evt.Quantity, evt.ProductId, evt.Id, item.Quantity);
            }

            await _processed.AddAsync(new ProcessedMessage
            {
                MessageId = messageId,
                MessageType = nameof(OrderCreatedEvent),
                ProcessedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
