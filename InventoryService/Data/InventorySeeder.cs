using InventoryService.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Data;

public static class InventorySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(InventorySeeder));

        await db.Database.MigrateAsync();

        if (await db.InventoryItems.AnyAsync())
        {
            logger.LogInformation("Inventory already seeded; skipping.");
            return;
        }

        var items = new[]
        {
            new InventoryItem { ProductId = 1, Quantity = 100 },
            new InventoryItem { ProductId = 2, Quantity = 50 },
            new InventoryItem { ProductId = 3, Quantity = 200 },
            new InventoryItem { ProductId = 4, Quantity = 25 },
            new InventoryItem { ProductId = 5, Quantity = 500 },
        };

        await db.InventoryItems.AddRangeAsync(items);
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} inventory items.", items.Length);
    }
}
