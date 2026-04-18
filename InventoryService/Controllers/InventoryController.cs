using InventoryService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _inventory;

    public InventoryController(IInventoryRepository inventory) => _inventory = inventory;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _inventory.GetAllAsync());

    [HttpGet("{productId:int}")]
    public async Task<IActionResult> Get(int productId)
    {
        var item = await _inventory.GetAsync(productId);
        return item is null ? NotFound() : Ok(item);
    }
}
