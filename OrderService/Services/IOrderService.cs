
namespace OrderService.Services
{
    public interface IOrderService
    {
        Task CreateOrderAsync(int productId, int quantity);
    }
}