using System.Diagnostics;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Services;
using kütüphaneuygulaması.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly kütüphaneuygulaması.Data.kütüphaneuygulamasıContext _context;
        private readonly ISearchService _searchService;
        private readonly ILoggingService _loggingService;

        public HomeController(ILogger<HomeController> logger, kütüphaneuygulaması.Data.kütüphaneuygulamasıContext context, 
                           ISearchService searchService, ILoggingService loggingService)
        {
            _logger = logger;
            _context = context;
            _searchService = searchService;
            _loggingService = loggingService;
        }

        public IActionResult Index()
        {
            // Ana sayfa yerine direkt kitap kataloğuna yönlendir
            return RedirectToAction("catalogue", "Books");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            try
            {
                ViewData["Title"] = "Hakkımızda";
                ViewBag.CompanyName = "Kütüphane Uygulaması";
                ViewBag.FoundedYear = 2024;
                ViewBag.Description = "Modern ve kullanıcı dostu kütüphane yönetim sistemi.";
                
                // İstatistikler
                ViewBag.TotalBooks = await _context.Book.Where(b => b.IsActive).CountAsync();
                ViewBag.TotalCategories = await _context.Category.Where(c => c.IsActive).CountAsync();
                ViewBag.TotalOrders = await _context.orders.CountAsync();
                
                TempData["AboutMessage"] = "Bilgiye erişim herkesin hakkıdır.";

                _loggingService.LogInformation("About page loaded successfully");
                return View();
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error loading about page");
                TempData["Error"] = "Hakkımızda sayfası yüklenirken bir hata oluştu.";
                return View();
            }
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "Bize Ulaşın";
            ViewBag.Email = "info@kutuphane.com";
            ViewBag.Phone = "+90 212 555 0123";
            ViewBag.Address = "İstanbul, Türkiye";
            ViewBag.WorkingHours = "Pazartesi - Cuma: 09:00 - 18:00";
            ViewBag.SocialMedia = new Dictionary<string, string>
            {
                { "Facebook", "https://facebook.com/kutuphane" },
                { "Twitter", "https://twitter.com/kutuphane" },
                { "Instagram", "https://instagram.com/kutuphane" }
            };
            
            TempData["ContactMessage"] = "Sorularınız için bizimle iletişime geçin.";
            return View();
        }

        // GET: Home/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "admin")
                    return RedirectToAction("Index");

                // Admin dashboard istatistikleri
                ViewBag.TotalBooks = await _context.Book.Where(b => b.IsActive).CountAsync();
                ViewBag.TotalCategories = await _context.Category.Where(c => c.IsActive).CountAsync();
                ViewBag.TotalOrders = await _context.orders.CountAsync();
                ViewBag.TotalUsers = await _context.usersaccounts.Where(u => u.IsActive).CountAsync();

                // Son siparişler
                ViewBag.RecentOrders = await _context.orders
                    .Include(o => o.Book)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.orderdate)
                    .Take(10)
                    .ToListAsync();

                // Düşük stok kitapları
                ViewBag.LowStockBooks = await _searchService.GetLowStockBooks(10);

                // Kategori bazında kitap sayıları
                ViewBag.CategoryStats = await _context.Category
                    .Where(c => c.IsActive)
                    .Include(c => c.Books)
                    .Select(c => new { c.Name, BookCount = c.Books.Count })
                    .ToListAsync();

                _loggingService.LogUserAction(HttpContext.Session.GetString("userid") ?? "0", "Dashboard", "Admin dashboard accessed");
                return View();
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        // GET: Home/Statistics
        public async Task<IActionResult> Statistics()
        {
            try
            {
                if (HttpContext.Session.GetString("Role") != "admin")
                    return RedirectToAction("Index");

                // Detaylı istatistikler
                var totalSales = await _context.orders.SumAsync(o => o.TotalPrice);
                var averageOrderValue = await _context.orders.AverageAsync(o => o.TotalPrice);
                var totalBooksSold = await _context.orders.SumAsync(o => o.quantity);

                ViewBag.TotalSales = totalSales;
                ViewBag.AverageOrderValue = averageOrderValue;
                ViewBag.TotalBooksSold = totalBooksSold;

                // Aylık satış grafiği için veri
                var monthlySales = await _context.orders
                    .GroupBy(o => new { o.orderdate.Year, o.orderdate.Month })
                    .Select(g => new { 
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}", 
                        Total = g.Sum(o => o.TotalPrice),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .Take(12)
                    .ToListAsync();

                ViewBag.MonthlySales = monthlySales;

                _loggingService.LogUserAction(HttpContext.Session.GetString("userid") ?? "0", "Statistics", "Statistics page accessed");
                return View();
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error loading statistics");
                TempData["Error"] = "İstatistikler yüklenirken bir hata oluştu.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Home/Error
        public IActionResult Error(string? traceId = null)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
            var isDevelopment = environment?.IsDevelopment() ?? false;
            
            ViewBag.RequestId = requestId;
            ViewBag.TraceId = traceId;
            ViewBag.ErrorMessage = TempData["ErrorMessage"]?.ToString() ?? "Beklenmeyen bir hata oluştu.";
            ViewBag.ErrorDetails = TempData["ErrorDetails"]?.ToString();
            ViewBag.IsDevelopment = isDevelopment;

            // Development modunda daha detaylı bilgi
            if (isDevelopment)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"]?.ToString() ?? "Beklenmeyen bir hata oluştu.";
                ViewBag.ErrorDetails = TempData["ErrorDetails"]?.ToString() ?? "Hata detayları mevcut değil.";
            }

            _loggingService.LogError(new Exception(ViewBag.ErrorMessage), "Error page displayed. RequestId: {RequestId}", requestId);
            
            return View(new ErrorViewModel { RequestId = requestId });
        }

        // GET: Home/NotFound
        public new IActionResult NotFound()
        {
            Response.StatusCode = 404;
            ViewBag.ErrorMessage = "Aradığınız sayfa bulunamadı.";
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _loggingService.LogWarning("404 Not Found - URL: {Url}", HttpContext.Request.Path);
            
            return View("Error");
        }

        // GET: Home/Unauthorized
        public new IActionResult Unauthorized()
        {
            Response.StatusCode = 401;
            ViewBag.ErrorMessage = "Bu sayfaya erişim yetkiniz bulunmamaktadır.";
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _loggingService.LogSecurityEvent("Unauthorized Access", 
                HttpContext.Session.GetString("userid") ?? "Anonymous", 
                $"Attempted to access: {HttpContext.Request.Path}");
            
            return View("Error");
        }

        // GET: Home/ServerError
        public IActionResult ServerError()
        {
            Response.StatusCode = 500;
            ViewBag.ErrorMessage = "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.";
            ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            _loggingService.LogCritical(new Exception("Server Error"), "500 Server Error - RequestId: {RequestId}", ViewBag.RequestId);
            
            return View("Error");
        }

        // GET: Home/UserFavorites
        public async Task<IActionResult> UserFavorites()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return Json(new { Success = false, Message = "Kullanıcı girişi gerekli" });

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            var favorites = await _context.Favorite
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .ThenInclude(b => b.Category)
                .OrderByDescending(f => f.AddedDate)
                .Take(5)
                .ToListAsync();

            var result = favorites.Select(f => new
            {
                Id = f.Id,
                BookId = f.BookId,
                BookTitle = f.Book.title,
                BookAuthor = f.Book.author,
                BookPrice = f.Book.price,
                BookImage = f.Book.imgfile,
                CategoryName = f.Book.Category?.Name,
                AddedDate = f.AddedDate.ToString("dd.MM.yyyy"),
                TimeAgo = f.TimeAgo
            }).ToList();

            return Json(new { Success = true, Favorites = result, Count = favorites.Count });
        }


    }
}
