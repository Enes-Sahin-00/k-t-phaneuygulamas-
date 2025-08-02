using Microsoft.AspNetCore.Mvc;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Models.DTOs;
using kütüphaneuygulaması.Services;
using kütüphaneuygulaması.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace kütüphaneuygulaması.Controllers.Api
{
    /// <summary>
    /// Kitaplar için RESTful API endpoint'leri
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BooksApiController : ControllerBase
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly ISearchService _searchService;
        private readonly ICacheService _cacheService;
        private readonly ILoggingService _loggingService;
        private readonly IValidationService _validationService;

        public BooksApiController(
            kütüphaneuygulamasıContext context,
            ISearchService searchService,
            ICacheService cacheService,
            ILoggingService loggingService,
            IValidationService validationService)
        {
            _context = context;
            _searchService = searchService;
            _cacheService = cacheService;
            _loggingService = loggingService;
            _validationService = validationService;
        }

        /// <summary>
        /// Tüm aktif kitapları sayfalı olarak listeler
        /// </summary>
        /// <param name="page">Sayfa numarası (varsayılan: 1, minimum: 1)</param>
        /// <param name="pageSize">Sayfa başına kayıt sayısı (varsayılan: 20, maksimum: 100)</param>
        /// <returns>Sayfalı kitap listesi</returns>
        /// <response code="200">Kitaplar başarıyla getirildi</response>
        /// <response code="400">Geçersiz sayfa parametreleri</response>
        /// <response code="500">Sunucu hatası</response>
        /// <example>
        /// GET /api/books?page=1&pageSize=10
        /// </example>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<Book>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<Book>>>> GetBooks(int page = 1, int pageSize = 20)
        {
            try
            {
                var cacheKey = $"books:page:{page}:size:{pageSize}";
                var books = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    var skip = (page - 1) * pageSize;
                    return await _context.Book
                        .Include(b => b.Category)
                        .Where(b => b.IsActive)
                        .OrderByDescending(b => b.CreatedDate)
                        .Skip(skip)
                        .Take(pageSize)
                        .ToListAsync();
                }, TimeSpan.FromMinutes(15));

                _loggingService.LogApiCall("/api/books", "GET", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<List<Book>>
                {
                    Success = true,
                    Message = "Kitaplar başarıyla getirildi",
                    Data = books,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitaplar getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kitaplar getirilirken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Belirtilen ID'ye sahip kitabın detaylarını getirir
        /// </summary>
        /// <param name="id">Kitap ID'si (pozitif tam sayı)</param>
        /// <returns>Kitap detayları ve kategori bilgisi</returns>
        /// <response code="200">Kitap başarıyla getirildi</response>
        /// <response code="400">Geçersiz ID formatı</response>
        /// <response code="404">Kitap bulunamadı</response>
        /// <response code="500">Sunucu hatası</response>
        /// <example>
        /// GET /api/books/1
        /// </example>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<Book>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<Book>>> GetBook(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçersiz kitap ID'si",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var cacheKey = $"book:details:{id}";
                var book = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _context.Book
                        .Include(b => b.Category)
                        .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
                }, TimeSpan.FromMinutes(30));

                if (book == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kitap bulunamadı",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _loggingService.LogApiCall($"/api/books/{id}", "GET", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<Book>
                {
                    Success = true,
                    Message = "Kitap başarıyla getirildi",
                    Data = book,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitap getirilirken hata oluştu: {BookId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kitap getirilirken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Yeni kitap oluşturur (Sadece Admin)
        /// </summary>
        /// <param name="bookDto">Kitap bilgileri</param>
        /// <returns>Oluşturulan kitap</returns>
        /// <response code="201">Kitap başarıyla oluşturuldu</response>
        /// <response code="400">Geçersiz kitap bilgileri</response>
        /// <response code="401">Yetkilendirme gerekli</response>
        /// <response code="403">Admin yetkisi gerekli</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<Book>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<Book>>> CreateBook([FromBody] CreateBookDto bookDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçersiz kitap bilgileri",
                        Errors = errors,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Input validation
                if (!_validationService.ValidateBookData(bookDto))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kitap verileri doğrulanamadı",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var book = new Book
                {
                    title = bookDto.Title,
                    author = bookDto.Author,
                    info = bookDto.Info,
                    price = bookDto.Price,
                    bookquantity = bookDto.BookQuantity,
                    cataid = bookDto.CategoryId,
                    ISBN = bookDto.ISBN,
                    PageCount = bookDto.PageCount,
                    PublicationDate = bookDto.PublicationDate,
                    Language = bookDto.Language ?? "Türkçe",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Book.Add(book);
                await _context.SaveChangesAsync();

                // Cache'i temizle
                await _cacheService.RemoveAsync("books:*");

                _loggingService.LogApiCall("/api/books", "POST", 201, TimeSpan.Zero);

                return CreatedAtAction(nameof(GetBook), new { id = book.Id }, new ApiResponse<Book>
                {
                    Success = true,
                    Message = "Kitap başarıyla oluşturuldu",
                    Data = book,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitap oluşturulurken hata oluştu");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kitap oluşturulurken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Kitap bilgilerini günceller (Sadece Admin)
        /// </summary>
        /// <param name="id">Kitap ID'si</param>
        /// <param name="updateDto">Güncellenecek bilgiler</param>
        /// <returns>Güncellenmiş kitap</returns>
        /// <response code="200">Kitap başarıyla güncellendi</response>
        /// <response code="400">Geçersiz güncelleme bilgileri</response>
        /// <response code="401">Yetkilendirme gerekli</response>
        /// <response code="403">Admin yetkisi gerekli</response>
        /// <response code="404">Kitap bulunamadı</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<Book>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<Book>>> UpdateBook(int id, [FromBody] UpdateBookDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Geçersiz güncelleme bilgileri",
                        Errors = errors,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var book = await _context.Book
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

                if (book == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kitap bulunamadı",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Güncelleme
                if (!string.IsNullOrEmpty(updateDto.Title))
                    book.title = updateDto.Title;
                if (!string.IsNullOrEmpty(updateDto.Author))
                    book.author = updateDto.Author;
                if (!string.IsNullOrEmpty(updateDto.Info))
                    book.info = updateDto.Info;
                if (updateDto.Price.HasValue)
                    book.price = updateDto.Price.Value;
                if (updateDto.BookQuantity.HasValue)
                    book.bookquantity = updateDto.BookQuantity.Value;
                if (updateDto.CategoryId.HasValue)
                    book.cataid = updateDto.CategoryId.Value;
                if (!string.IsNullOrEmpty(updateDto.ISBN))
                    book.ISBN = updateDto.ISBN;
                if (updateDto.PageCount.HasValue)
                    book.PageCount = updateDto.PageCount;
                if (updateDto.PublicationDate.HasValue)
                    book.PublicationDate = updateDto.PublicationDate;
                if (!string.IsNullOrEmpty(updateDto.Language))
                    book.Language = updateDto.Language;

                book.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Cache'i temizle
                await _cacheService.RemoveAsync($"book:details:{id}");
                await _cacheService.RemoveAsync("books:*");

                _loggingService.LogApiCall($"/api/books/{id}", "PUT", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<Book>
                {
                    Success = true,
                    Message = "Kitap başarıyla güncellendi",
                    Data = book,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitap güncellenirken hata oluştu: {BookId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kitap güncellenirken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Kitabı siler (Sadece Admin)
        /// </summary>
        /// <param name="id">Kitap ID'si</param>
        /// <returns>Silme sonucu</returns>
        /// <response code="200">Kitap başarıyla silindi</response>
        /// <response code="401">Yetkilendirme gerekli</response>
        /// <response code="403">Admin yetkisi gerekli</response>
        /// <response code="404">Kitap bulunamadı</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBook(int id)
        {
            try
            {
                var book = await _context.Book
                    .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);

                if (book == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Kitap bulunamadı",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Soft delete
                book.IsActive = false;
                book.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Cache'i temizle
                await _cacheService.RemoveAsync($"book:details:{id}");
                await _cacheService.RemoveAsync("books:*");

                _loggingService.LogApiCall($"/api/books/{id}", "DELETE", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Kitap başarıyla silindi",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitap silinirken hata oluştu: {BookId}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Kitap silinirken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Kitaplarda arama yapar
        /// </summary>
        /// <param name="searchTerm">Arama terimi</param>
        /// <param name="page">Sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
        /// <returns>Arama sonuçları</returns>
        /// <response code="200">Arama sonuçları başarıyla getirildi</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SearchResult<Book>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<SearchResult<Book>>>> SearchBooks(
            [FromQuery] string searchTerm = "", 
            int page = 1, 
            int pageSize = 20)
        {
            try
            {
                var searchResult = await _searchService.SearchBooks(searchTerm, null, null, null, null, page, pageSize);

                _loggingService.LogApiCall("/api/books/search", "GET", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<SearchResult<Book>>
                {
                    Success = true,
                    Message = "Arama sonuçları başarıyla getirildi",
                    Data = searchResult,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Kitap araması sırasında hata oluştu");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Arama sırasında bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Popüler kitapları getirir
        /// </summary>
        /// <param name="count">Getirilecek kitap sayısı</param>
        /// <returns>Popüler kitaplar</returns>
        /// <response code="200">Popüler kitaplar başarıyla getirildi</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpGet("popular")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<Book>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<ActionResult<ApiResponse<List<Book>>>> GetPopularBooks(int count = 10)
        {
            try
            {
                var cacheKey = $"popular:books:{count}";
                var popularBooks = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _searchService.GetPopularBooks(count);
                }, TimeSpan.FromMinutes(30));

                _loggingService.LogApiCall("/api/books/popular", "GET", 200, TimeSpan.Zero);

                return Ok(new ApiResponse<List<Book>>
                {
                    Success = true,
                    Message = "Popüler kitaplar başarıyla getirildi",
                    Data = popularBooks,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Popüler kitaplar getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Popüler kitaplar getirilirken bir hata oluştu",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 