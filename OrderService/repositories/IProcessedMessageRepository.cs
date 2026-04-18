using OrderService.Entities;

namespace OrderService.repositories;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(string messageId);
    Task AddAsync(ProcessedMessage message);
}
