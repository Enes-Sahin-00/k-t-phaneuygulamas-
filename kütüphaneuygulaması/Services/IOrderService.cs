using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Services
{
    public interface IOrderService
    {
        Task<orders> CreateOrder(int userId, int bookId, int quantity, string? deliveryAddress = null, string? phoneNumber = null, string? notes = null);
        Task<bool> UpdateOrderStatus(int orderId, OrderStatus status);
        Task<List<orders>> GetUserOrders(int userId);
        Task<List<orders>> GetAllOrders();
        Task<orders?> GetOrderById(int orderId);
        Task<decimal> CalculateOrderTotal(int bookId, int quantity);
        Task<bool> ProcessCheckout(int userId);
        Task<List<orders>> GetOrdersByStatus(OrderStatus status);
        Task<Dictionary<OrderStatus, int>> GetOrderStatusCounts();
    }
} 