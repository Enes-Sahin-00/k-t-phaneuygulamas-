using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly kütüphaneuygulamasıContext _context;

        public InventoryService(kütüphaneuygulamasıContext context)
        {
            _context = context;
        }

        public async Task<bool> IsBookInStock(int bookId, int quantity = 1)
        {
            var book = await _context.Book.FindAsync(bookId);
            if (book == null || !book.IsActive)
                return false;

            return book.bookquantity >= quantity;
        }

        public async Task<bool> ReserveStock(int bookId, int quantity)
        {
            var book = await _context.Book.FindAsync(bookId);
            if (book == null || book.bookquantity < quantity)
                return false;

            book.bookquantity -= quantity;
            book.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseStock(int bookId, int quantity)
        {
            var book = await _context.Book.FindAsync(bookId);
            if (book == null)
                return false;

            book.bookquantity += quantity;
            book.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStock(int bookId, int quantity)
        {
            var book = await _context.Book.FindAsync(bookId);
            if (book == null)
                return false;

            book.bookquantity = quantity;
            book.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetAvailableStock(int bookId)
        {
            var book = await _context.Book.FindAsync(bookId);
            return book?.bookquantity ?? 0;
        }

        public async Task<List<Book>> GetLowStockBooks(int threshold = 5)
        {
            return await _context.Book
                .Where(b => b.IsActive && b.bookquantity <= threshold)
                .Include(b => b.Category)
                .ToListAsync();
        }

        public async Task<bool> CanAddToCart(int bookId, int quantity, int userId)
        {
            // Kitap stokta mı kontrol et
            if (!await IsBookInStock(bookId, quantity))
                return false;

            // Kullanıcının sepetinde bu kitap var mı kontrol et
            var existingCartItem = await _context.Cart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);

            if (existingCartItem != null)
            {
                // Mevcut miktar + yeni miktar stoktan fazla mı?
                var totalQuantity = existingCartItem.Quantity + quantity;
                return await IsBookInStock(bookId, totalQuantity);
            }

            return true;
        }
    }
} 