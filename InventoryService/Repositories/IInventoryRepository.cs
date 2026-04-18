using InventoryService.Entities;

namespace InventoryService.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetAsync(int productId);
    Task AddAsync(InventoryItem item);
    Task<List<InventoryItem>> GetAllAsync();
}
