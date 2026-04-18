namespace shared.Contract;

public class InventoryReservationFailedEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
