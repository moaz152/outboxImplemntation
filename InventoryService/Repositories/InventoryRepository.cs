using InventoryService.Data;
using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _db;

    public InventoryRepository(InventoryDbContext db) => _db = db;

    public Task<InventoryItem?> GetAsync(int productId)
        => _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);

    public async Task AddAsync(InventoryItem item)
        => await _db.InventoryItems.AddAsync(item);

    public Task<List<InventoryItem>> GetAllAsync()
        => _db.InventoryItems.ToListAsync();
}
