using shared.Contract;

namespace OrderService.Services;

public interface IInventoryFailedHandler
{
    Task HandleAsync(string messageId, InventoryReservationFailedEvent evt, CancellationToken ct);
}
