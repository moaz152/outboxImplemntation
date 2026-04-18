using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Entities;

namespace OrderService.repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly OrderDbContext _context;
        public OutboxRepository(OrderDbContext context)
        {
            _context = context;
        }
        public async Task AddOutboxMessageAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
           
        }

        public Task<List<OutboxMessage>> GetUnprocessedBatchAsync(int batchSize)
        => _context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync();


        public async Task MarkProcessedAsync(int id)
        {
            var msg = await _context.OutboxMessages.FindAsync( id );
            if (msg is null) return;
            msg.ProcessedAt = DateTime.UtcNow;
            msg.error = null;
            await _context.SaveChangesAsync();
        }

        public async Task MarkFailedAsync(int id, string error)
        {
            var msg = await _context.OutboxMessages.FindAsync( id );
            if (msg is null) return;
            msg.error = error;
            msg.retryCount += 1;
            await _context.SaveChangesAsync();
        }

    }
}
