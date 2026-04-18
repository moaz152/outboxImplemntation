using Microsoft.AspNetCore.Mvc;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
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
}
