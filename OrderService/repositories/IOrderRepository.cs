using OrderService.Entities;

namespace OrderService.repositories
{
    public interface IOrderRepository
    {
        Task AddOrderAsync(Order order);
    }
}