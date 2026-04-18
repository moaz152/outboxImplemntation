using InventoryService.Entities;

namespace InventoryService.Repositories;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(string messageId);
    Task AddAsync(ProcessedMessage message);
}
