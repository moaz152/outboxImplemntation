namespace OrderService.Entities
{
    public class OutboxMessage
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? error { get; set; } 
        public string retryCount { get; set; } = "0";
    }
}
