using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Services;

namespace kütüphaneuygulaması.Controllers
{
    public class BooksController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly ISearchService _searchService;
        private readonly ICacheService _cacheService;
        private readonly IDatabaseOptimizationService _dbOptimizationService;
        private readonly IImageOptimizationService _imageOptimizationService;

        public BooksController(kütüphaneuygulamasıContext context, ISearchService searchService, 
                           ICacheService cacheService, IDatabaseOptimizationService dbOptimizationService,
                           IImageOptimizationService imageOptimizationService)
        {
            _context = context;
            _searchService = searchService;
            _cacheService = cacheService;
            _dbOptimizationService = dbOptimizationService;
            _imageOptimizationService = imageOptimizationService;
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            var cacheKey = "admin:books:list";
            var books = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _context.Book
                    .Include(b => b.Category)
                    .Where(b => b.IsActive)
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync();
            }, TimeSpan.FromMinutes(10));

            return View(books);
        }

        // GET: Books/Catalogue
        public async Task<IActionResult> catalogue(string? searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string? author, int page = 1)
        {
            // Arama ve filtreleme parametrelerini ViewBag'e ekle
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Author = author;
            ViewBag.CurrentPage = page;

            // Kategorileri cache'den al
            var categoriesCacheKey = "categories:active";
            var categories = await _cacheService.GetOrSetAsync(categoriesCacheKey, async () =>
            {
                return await _context.Category.Where(c => c.IsActive).ToListAsync();
            }, TimeSpan.FromMinutes(30));

            ViewBag.Categories = categories;

            // Basit arama ve filtreleme
            var query = _context.Book
                .Include(b => b.Category)
                .Where(b => b.IsActive);

            // Arama terimi
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b => b.title.Contains(searchTerm) || b.author.Contains(searchTerm) || b.info.Contains(searchTerm));
            }

            // Kategori filtresi
            if (categoryId.HasValue)
            {
                query = query.Where(b => b.cataid == categoryId.Value);
            }

            // Fiyat filtresi
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
                query = query.Where(b => b.author.Contains(author));
            }

            // Toplam sayfa sayısını hesapla
            var totalBooks = await query.CountAsync();
            var pageSize = 12;
            var totalPages = (int)Math.Ceiling((double)totalBooks / pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalBooks;

            // Sayfalama
            var books = await query
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(books);
        }

        // GET: Books/Search
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { suggestions = new List<string>() });

            var cacheKey = $"search:suggestions:{term.ToLower()}";
            var suggestions = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var suggestionsData = await _searchService.GetSearchSuggestions(term);
                return suggestionsData.Keys.ToList();
            }, TimeSpan.FromMinutes(5));

            return Json(new { suggestions });
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cacheKey = $"book:details:{id}";
            var book = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _context.Book
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }, TimeSpan.FromMinutes(15));

            if (book == null)
            {
                return NotFound();
            }

            // Benzer kitapları cache'den al
            var similarBooksCacheKey = $"similar:books:{id}";
            var similarBooks = await _cacheService.GetOrSetAsync(similarBooksCacheKey, async () =>
            {
                return await _context.Book
                    .Include(b => b.Category)
                    .Where(b => b.IsActive && b.cataid == book.cataid && b.Id != book.Id)
                    .Take(4)
                    .ToListAsync();
            }, TimeSpan.FromMinutes(30));

            ViewBag.SimilarBooks = similarBooks;

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile file, [Bind("Id,title,info,bookquantity,price,cataid,author,ISBN,PageCount,PublicationDate,Language")] Book book)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    // Image optimization
                    if (await _imageOptimizationService.IsImageFileAsync(file))
                    {
                        var optimizedFileName = await _imageOptimizationService.OptimizeImageAsync(file, 800, 600, 85);
                        book.imgfile = optimizedFileName;
                    }
                    else
                    {
                        TempData["Error"] = "Geçersiz dosya formatı. Lütfen geçerli bir resim dosyası seçin.";
                        ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
                        return View(book);
                    }
                }

                book.CreatedDate = DateTime.Now;
                book.IsActive = true;

                _context.Add(book);
                await _context.SaveChangesAsync();

                // Invalidate related cache
                await _cacheService.RemoveAsync("admin:books:list");
                await _cacheService.RemoveAsync("popular:books:5");
                await _cacheService.RemoveAsync("newest:books:5");
                await _cacheService.InvalidateCategoryAsync("books");

                TempData["Success"] = "Kitap başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (id == null)
            {
                return NotFound();
            }

            var cacheKey = $"book:edit:{id}";
            var book = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _context.Book.FindAsync(id);
            }, TimeSpan.FromMinutes(5));

            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IFormFile file, int id, [Bind("Id,title,info,bookquantity,price,cataid,author,imgfile,ISBN,PageCount,PublicationDate,Language")] Book book)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Book.FindAsync(id);
                    if (existingBook == null)
                        return NotFound();

                    if (file != null)
                    {
                        // Image optimization
                        if (await _imageOptimizationService.IsImageFileAsync(file))
                        {
                            var optimizedFileName = await _imageOptimizationService.OptimizeImageAsync(file, 800, 600, 85);
                            existingBook.imgfile = optimizedFileName;
                        }
                        else
                        {
                            TempData["Error"] = "Geçersiz dosya formatı. Lütfen geçerli bir resim dosyası seçin.";
                            ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
                            return View(book);
                        }
                    }

                    existingBook.title = book.title;
                    existingBook.info = book.info;
                    existingBook.bookquantity = book.bookquantity;
                    existingBook.price = book.price;
                    existingBook.cataid = book.cataid;
                    existingBook.author = book.author;
                    existingBook.ISBN = book.ISBN;
                    existingBook.PageCount = book.PageCount;
                    existingBook.PublicationDate = book.PublicationDate;
                    existingBook.Language = book.Language;
                    existingBook.UpdatedDate = DateTime.Now;

                    _context.Update(existingBook);
                    await _context.SaveChangesAsync();

                    // Invalidate related cache
                    await _cacheService.RemoveAsync($"book:details:{id}");
                    await _cacheService.RemoveAsync($"book:edit:{id}");
                    await _cacheService.RemoveAsync("admin:books:list");
                    await _cacheService.RemoveAsync("popular:books:5");
                    await _cacheService.RemoveAsync("newest:books:5");
                    await _cacheService.InvalidateCategoryAsync("books");

                    TempData["Success"] = "Kitap başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Categories = new SelectList(_context.Category.Where(c => c.IsActive), "Id", "Name");
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (id == null)
            {
                return NotFound();
            }

            var cacheKey = $"book:delete:{id}";
            var book = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                return await _context.Book
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }, TimeSpan.FromMinutes(5));

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            var book = await _context.Book.FindAsync(id);
            if (book != null)
            {
                // Soft delete - sadece IsActive'i false yap
                book.IsActive = false;
                book.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // Invalidate related cache
                await _cacheService.RemoveAsync($"book:details:{id}");
                await _cacheService.RemoveAsync($"book:edit:{id}");
                await _cacheService.RemoveAsync($"book:delete:{id}");
                await _cacheService.RemoveAsync("admin:books:list");
                await _cacheService.RemoveAsync("popular:books:5");
                await _cacheService.RemoveAsync("newest:books:5");
                await _cacheService.InvalidateCategoryAsync("books");

                TempData["Success"] = "Kitap başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Books/UpdateImages
        public async Task<IActionResult> UpdateImages()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            var books = await _context.Book.Take(3).ToListAsync();
            return View(books);
        }

        // POST: Books/UpdateImages
        [HttpPost]
        public async Task<IActionResult> UpdateImages(List<int> bookIds)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            var imageUrls = new List<string>
            {
                "https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg",
                "https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13",
                "https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg"
            };

            for (int i = 0; i < Math.Min(bookIds.Count, imageUrls.Count); i++)
            {
                var book = await _context.Book.FindAsync(bookIds[i]);
                if (book != null)
                {
                    book.imgfile = imageUrls[i];
                    book.UpdatedDate = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Kitap resimleri başarıyla güncellendi!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Books/UpdateImagesDirect
        public async Task<IActionResult> UpdateImagesDirect()
        {
            try
            {
                // İlk 3 kitabı al
                var books = await _context.Book.Take(3).ToListAsync();
                
                if (books.Count >= 3)
                {
                    // Resim linklerini güncelle
                    books[0].imgfile = "https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg";
                    books[1].imgfile = "https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13";
                    books[2].imgfile = "https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg";
                    
                    // Güncelleme tarihlerini ayarla
                    books[0].UpdatedDate = DateTime.Now;
                    books[1].UpdatedDate = DateTime.Now;
                    books[2].UpdatedDate = DateTime.Now;
                    
                    await _context.SaveChangesAsync();
                    
                    return Json(new { 
                        success = true, 
                        message = "3 kitabın resimleri başarıyla güncellendi!",
                        books = books.Select(b => new { 
                            Id = b.Id, 
                            Title = b.title, 
                            Author = b.author, 
                            ImageUrl = b.imgfile 
                        })
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "Veritabanında 3 kitap bulunamadı!" 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Hata oluştu: {ex.Message}" 
                });
            }
        }

        // GET: Books/ListBooks
        public async Task<IActionResult> ListBooks()
        {
            var books = await _context.Book.Take(10).ToListAsync();
            return Json(books.Select(b => new { 
                Id = b.Id, 
                Title = b.title, 
                Author = b.author, 
                ImageUrl = b.imgfile 
            }));
        }

        private bool BookExists(int id)
        {
            return _context.Book.Any(e => e.Id == id);
        }
    }
}
