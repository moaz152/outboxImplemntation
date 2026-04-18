using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;
using OrderService.repositories;
using shared.Contract;

namespace OrderService.Services;

public class InventoryFailedHandler : IInventoryFailedHandler
{
    private readonly OrderDbContext _db;
    private readonly IProcessedMessageRepository _processed;
    private readonly ILogger<InventoryFailedHandler> _logger;

    public InventoryFailedHandler(
        OrderDbContext db,
        IProcessedMessageRepository processed,
        ILogger<InventoryFailedHandler> logger)
    {
        _db = db;
        _processed = processed;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, InventoryReservationFailedEvent evt, CancellationToken ct)
    {
        if (await _processed.ExistsAsync(messageId))
        {
            _logger.LogInformation("Skipping already-processed compensation {MessageId}", messageId);
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, ct);
            if (order is null)
            {
                _logger.LogWarning("Compensation for unknown OrderId={OrderId}; recording as processed", evt.OrderId);
            }
            else
            {
                order.Status = OrderStatus.Cancelled;
                _logger.LogWarning(
                    "Cancelled OrderId={OrderId} — inventory reservation failed: {Reason}",
                    evt.OrderId, evt.Reason);
            }

            await _processed.AddAsync(new ProcessedMessage
            {
                MessageId = messageId,
                MessageType = nameof(InventoryReservationFailedEvent),
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
