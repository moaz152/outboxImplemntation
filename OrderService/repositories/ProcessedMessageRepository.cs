using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;

namespace OrderService.repositories;

public class ProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly OrderDbContext _db;

    public ProcessedMessageRepository(OrderDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string messageId)
        => _db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId);

    public async Task AddAsync(ProcessedMessage message)
        => await _db.ProcessedMessages.AddAsync(message);
}
