using InventoryService.Entities;

namespace InventoryService.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task<List<OutboxMessage>> GetUnprocessedBatchAsync(int batchSize);
    Task MarkProcessedAsync(int id);
    Task MarkFailedAsync(int id, string error);
}
