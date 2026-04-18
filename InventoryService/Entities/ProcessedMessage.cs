namespace InventoryService.Entities;

// Inbox row — one per consumed message, used for idempotency.
public class ProcessedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
