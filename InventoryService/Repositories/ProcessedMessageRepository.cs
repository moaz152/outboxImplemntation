using InventoryService.Data;
using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories;

public class ProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly InventoryDbContext _db;

    public ProcessedMessageRepository(InventoryDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string messageId)
        => _db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId);

    public async Task AddAsync(ProcessedMessage message)
        => await _db.ProcessedMessages.AddAsync(message);
}
