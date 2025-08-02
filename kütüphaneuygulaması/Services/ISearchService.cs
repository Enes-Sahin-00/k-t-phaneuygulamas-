using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Services
{
    public interface ISearchService
    {
        Task<SearchResult<Book>> SearchBooks(string? searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string? author, int page = 1, int pageSize = 12);
        Task<List<Book>> GetBooksByCategory(int categoryId);
        Task<List<Book>> GetBooksByAuthor(string author);
        Task<List<Book>> GetBooksByPriceRange(decimal minPrice, decimal maxPrice);
        Task<List<Book>> GetNewestBooks(int count = 10);
        Task<List<Book>> GetPopularBooks(int count = 10);
        Task<List<Book>> GetLowStockBooks(int threshold = 5);
        Task<Dictionary<string, int>> GetSearchSuggestions(string term);
    }

    public class SearchResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
} 