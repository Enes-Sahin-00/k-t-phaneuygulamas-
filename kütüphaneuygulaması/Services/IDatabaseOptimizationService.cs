using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Services
{
    public interface IDatabaseOptimizationService
    {
        Task<List<Book>> GetBooksWithOptimizedQuery(int page = 1, int pageSize = 20);
        Task<List<orders>> GetOrdersWithOptimizedQuery(int userId, int page = 1, int pageSize = 20);
        Task<Dictionary<string, object>> GetDashboardStatistics();
        Task<List<Book>> GetPopularBooksOptimized(int count = 10);
        Task<List<Book>> GetNewestBooksOptimized(int count = 10);
        Task<List<Category>> GetCategoriesWithBookCount();
        Task<List<usersaccounts>> GetUsersWithOrderCount();
        Task<object> GetSalesAnalytics(DateTime startDate, DateTime endDate);
        Task CleanupOldData();
        Task OptimizeDatabase();
        Task<List<string>> GetDatabaseIndexes();
        Task<string> GetDatabaseSize();
    }
} 