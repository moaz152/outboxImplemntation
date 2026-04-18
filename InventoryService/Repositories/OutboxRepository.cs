using InventoryService.Data;
using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly InventoryDbContext _db;

    public OutboxRepository(InventoryDbContext db) => _db = db;

    public async Task AddAsync(OutboxMessage message)
        => await _db.OutboxMessages.AddAsync(message);

    public Task<List<OutboxMessage>> GetUnprocessedBatchAsync(int batchSize)
        => _db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

    public async Task MarkProcessedAsync(int id)
    {
        var msg = await _db.OutboxMessages.FindAsync(id);
        if (msg is null) return;
        msg.ProcessedAt = DateTime.UtcNow;
        msg.Error = null;
        await _db.SaveChangesAsync();
    }

    public async Task MarkFailedAsync(int id, string error)
    {
        var msg = await _db.OutboxMessages.FindAsync(id);
        if (msg is null) return;
        msg.Error = error;
        msg.RetryCount += 1;
        await _db.SaveChangesAsync();
    }
}
