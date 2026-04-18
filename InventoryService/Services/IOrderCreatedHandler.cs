using shared.Contract;

namespace InventoryService.Services;

public interface IOrderCreatedHandler
{
    Task HandleAsync(string messageId, OrderCreatedEvent evt, CancellationToken ct);
}
