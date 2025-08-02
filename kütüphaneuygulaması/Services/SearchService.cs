using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Services
{
    public class SearchService : ISearchService
    {
        private readonly kütüphaneuygulamasıContext _context;

        public SearchService(kütüphaneuygulamasıContext context)
        {
            _context = context;
        }

        public async Task<SearchResult<Book>> SearchBooks(string? searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string? author, int page = 1, int pageSize = 12)
        {
            var query = _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive);

            // Arama terimi
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(b => 
                    b.title.ToLower().Contains(searchTerm) ||
                    b.author.ToLower().Contains(searchTerm) ||
                    b.info.ToLower().Contains(searchTerm) ||
                    b.ISBN != null && b.ISBN.ToLower().Contains(searchTerm)
                );
            }

            // Kategori filtresi
            if (categoryId.HasValue)
            {
                query = query.Where(b => b.cataid == categoryId.Value);
            }

            // Fiyat aralığı
            if (minPrice.HasValue)
            {
                query = query.Where(b => b.price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(b => b.price <= maxPrice.Value);
            }

            // Yazar filtresi
            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(b => b.author.ToLower().Contains(author.ToLower()));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new SearchResult<Book>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<Book>> GetBooksByCategory(int categoryId)
        {
            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.cataid == categoryId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Book>> GetBooksByAuthor(string author)
        {
            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.author.ToLower().Contains(author.ToLower()))
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Book>> GetBooksByPriceRange(decimal minPrice, decimal maxPrice)
        {
            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.price >= minPrice && b.price <= maxPrice)
                .OrderBy(b => b.price)
                .ToListAsync();
        }

        public async Task<List<Book>> GetNewestBooks(int count = 10)
        {
            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Book>> GetPopularBooks(int count = 10)
        {
            // En çok sipariş edilen kitaplar
            var popularBookIds = await _context.orders
                .GroupBy(o => o.bookId)
                .Select(g => new { BookId = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(count)
                .Select(x => x.BookId)
                .ToListAsync();

            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && popularBookIds.Contains(b.Id))
                .ToListAsync();
        }

        public async Task<List<Book>> GetLowStockBooks(int threshold = 5)
        {
            return await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.bookquantity <= threshold)
                .OrderBy(b => b.bookquantity)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetSearchSuggestions(string term)
        {
            var suggestions = new Dictionary<string, int>();

            // Yazar önerileri
            var authors = await _context.Book
                .Where(b => b.IsActive && b.author.ToLower().Contains(term.ToLower()))
                .GroupBy(b => b.author)
                .Select(g => new { Author = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            foreach (var author in authors)
            {
                suggestions[author.Author] = author.Count;
            }

            // Kategori önerileri
            var categories = await _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive && b.Category.Name.ToLower().Contains(term.ToLower()))
                .GroupBy(b => b.Category.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            foreach (var category in categories)
            {
                suggestions[category.Category] = category.Count;
            }

            return suggestions;
        }
    }
} 