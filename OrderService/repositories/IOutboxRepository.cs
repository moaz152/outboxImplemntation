using OrderService.Entities;

namespace OrderService.repositories
{
    public interface IOutboxRepository
    {
        Task AddOutboxMessageAsync(OutboxMessage message);
        Task<List<OutboxMessage>> GetUnprocessedBatchAsync(int batchSize);
        Task MarkProcessedAsync(int id);
        Task MarkFailedAsync(int id, string error);
    }
}