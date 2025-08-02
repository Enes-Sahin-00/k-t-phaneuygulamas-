using Microsoft.AspNetCore.Mvc;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Services;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Controllers
{
    /// <summary>
    /// Bu controller, değişken kapsamı ve best practice örneklerini gösterir
    /// </summary>
    public class ExampleController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly ISearchService _searchService;
        private readonly ICacheService _cacheService;
        private readonly IInventoryService _inventoryService;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;

        public ExampleController(
            kütüphaneuygulamasıContext context,
            ISearchService searchService,
            ICacheService cacheService,
            IInventoryService inventoryService,
            IOrderService orderService,
            INotificationService notificationService)
        {
            _context = context;
            _searchService = searchService;
            _cacheService = cacheService;
            _inventoryService = inventoryService;
            _orderService = orderService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// ✅ Doğru: Dependency Injection ile servis kullanımı
        /// </summary>
        public async Task<IActionResult> ProperServiceUsage()
        {
            // Servisler zaten inject edilmiş, doğrudan kullanabiliriz
            var searchResults = await _searchService.SearchBooks("programlama", null, null, null, null, 1, 12);
            var cachedData = await _cacheService.GetAsync<List<Book>>("popular_books");
            
            return View(searchResults);
        }

        /// <summary>
        /// ✅ Doğru: Değişken kapsamı ve null check
        /// </summary>
        public async Task<IActionResult> ProperVariableScope()
        {
            // Değişkeni kullanmadan önce tanımla
            List<Book> books;
            
            try
            {
                // Cache'den veri almayı dene
                books = await _cacheService.GetAsync<List<Book>>("books_list") ?? new List<Book>();
                
                // Null check yap
                if (books.Count == 0)
                {
                    // Cache'de yoksa veritabanından al
                    books = await _context.Book
                        .Where(b => b.IsActive)
                        .ToListAsync();
                    
                    // Cache'e kaydet
                    await _cacheService.SetAsync("books_list", books, TimeSpan.FromMinutes(10));
                }
                
                return View(books);
            }
            catch (Exception)
            {
                // Hata durumunda boş liste döndür
                books = new List<Book>();
                return View(books);
            }
        }

        /// <summary>
        /// ✅ Doğru: Conditional logic ile değişken kapsamı
        /// </summary>
        public async Task<IActionResult> ConditionalVariableScope(string searchTerm)
        {
            // Değişkeni dışarıda tanımla
            SearchResult<Book> searchResult;
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Arama terimi varsa arama yap
                searchResult = await _searchService.SearchBooks(searchTerm, null, null, null, null, 1, 12);
            }
            else
            {
                // Arama terimi yoksa tüm kitapları getir
                var allBooks = await _context.Book
                    .Where(b => b.IsActive)
                    .ToListAsync();
                
                searchResult = new SearchResult<Book>
                {
                    Items = allBooks,
                    TotalCount = allBooks.Count,
                    Page = 1,
                    PageSize = allBooks.Count
                };
            }
            
            return View(searchResult);
        }

        /// <summary>
        /// ✅ Doğru: Using statement ile kaynak yönetimi
        /// </summary>
        public async Task<IActionResult> ProperResourceManagement()
        {
            // Using statement ile otomatik dispose
            using var scope = _context.Database.BeginTransaction();
            
            try
            {
                var book = new Book
                {
                    title = "Yeni Kitap",
                    author = "Yazar Adı",
                    price = 50.00m,
                    bookquantity = 10,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                
                _context.Book.Add(book);
                await _context.SaveChangesAsync();
                
                // Cache'i temizle
                await _cacheService.RemoveAsync("books_list");
                
                scope.Commit();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                scope.Rollback();
                return StatusCode(500, "İşlem başarısız");
            }
        }

        /// <summary>
        /// ✅ Doğru: Async/await pattern kullanımı
        /// </summary>
        public async Task<IActionResult> ProperAsyncUsage()
        {
            // Async metodları await ile çağır
            var popularBooks = await _searchService.GetPopularBooks(5);
            var newestBooks = await _searchService.GetNewestBooks(5);
            var lowStockBooks = await _searchService.GetLowStockBooks(10);
            
            var viewModel = new
            {
                PopularBooks = popularBooks,
                NewestBooks = newestBooks,
                LowStockBooks = lowStockBooks
            };
            
            return View(viewModel);
        }

        /// <summary>
        /// ✅ Doğru: Stok kontrolü ve bildirim
        /// </summary>
        public async Task<IActionResult> InventoryCheck(int bookId, int quantity)
        {
            // Stok kontrolü
            if (!await _inventoryService.CanAddToCart(bookId, quantity, 1))
            {
                TempData["Error"] = "Yetersiz stok";
                return RedirectToAction("Index");
            }
            
            // Sepete ekleme işlemi
            var cartItem = new Cart
            {
                UserId = 1,
                BookId = bookId,
                Quantity = quantity,
                AddedDate = DateTime.Now,
                CreatedDate = DateTime.Now
            };
            
            _context.Cart.Add(cartItem);
            await _context.SaveChangesAsync();
            
            // Bildirim gönder
            await _notificationService.SendEmail("user@example.com", "Sepete Eklendi", $"Kitap sepete eklendi. Miktar: {quantity}");
            
            TempData["Success"] = "Kitap sepete eklendi";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// ❌ Yanlış: Değişken tanımlanmamış (Örnek)
        /// Bu metod çalışmaz, sadece örnek amaçlı
        /// </summary>
        /*
        public async Task<IActionResult> WrongVariableUsage()
        {
            // HATA: 'media' tanımlı değil!
            media.GetItems();
            return View();
        }
        */

        /// <summary>
        /// ❌ Yanlış: Yanlış kapsam (Örnek)
        /// Bu metod çalışmaz, sadece örnek amaçlı
        /// </summary>
        /*
        public async Task<IActionResult> WrongScope()
        {
            if (true)
            {
                var media = new MediaService(); // Sadece bu blokta geçerli
            }
            media.DoSomething(); // HATA: 'media' burada yok!
            return View();
        }
        */

        /// <summary>
        /// ❌ Yanlış: Namespace eksik (Örnek)
        /// Bu metod çalışmaz, sadece örnek amaçlı
        /// </summary>
        /*
        public IActionResult WrongNamespace()
        {
            var media = new Media(); // HATA: 'Media' tanınmıyor
            return View();
        }
        */

        /// <summary>
        /// ❌ Yanlış: Yazım hatası (Örnek)
        /// Bu metod çalışmaz, sadece örnek amaçlı
        /// </summary>
        /*
        public async Task<IActionResult> WrongSpelling()
        {
            var medai = new MediaService(); // Yanlış yazım
            media.DoSomething(); // HATA: 'media' değil 'medai' yazdın!
            return View();
        }
        */
    }
} 