using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly OrderDbContext _db;

    public OrdersController(IOrderService orderService, OrderDbContext db)
    {
        _orderService = orderService;
        _db = db;
    }

    public record CreateOrderRequest(int ProductId, int Quantity);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
    {
        if (req.Quantity <= 0)
            return BadRequest("Quantity must be > 0");

        await _orderService.CreateOrderAsync(req.ProductId, req.Quantity);
        return Accepted();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Orders.OrderByDescending(o => o.CreatedAt).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound() : Ok(order);
    }
}
