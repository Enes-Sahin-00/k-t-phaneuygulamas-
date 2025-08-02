using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Services
{
    public interface IInventoryService
    {
        Task<bool> IsBookInStock(int bookId, int quantity = 1);
        Task<bool> ReserveStock(int bookId, int quantity);
        Task<bool> ReleaseStock(int bookId, int quantity);
        Task<bool> UpdateStock(int bookId, int quantity);
        Task<int> GetAvailableStock(int bookId);
        Task<List<Book>> GetLowStockBooks(int threshold = 5);
        Task<bool> CanAddToCart(int bookId, int quantity, int userId);
    }
} 