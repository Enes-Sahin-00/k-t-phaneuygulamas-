using Microsoft.AspNetCore.Mvc;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Services;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Controllers
{
    public class CartController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;

        public CartController(kütüphaneuygulamasıContext context, IInventoryService inventoryService, 
                           IOrderService orderService, INotificationService notificationService)
        {
            _context = context;
            _inventoryService = inventoryService;
            _orderService = orderService;
            _notificationService = notificationService;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            var cartItems = await _context.Cart
                .Where(c => c.UserId == userId)
                .Include(c => c.Book)
                .ToListAsync();

            var enrichedCart = cartItems.Select(c => new
            {
                c.Id,
                c.BookId,
                BookTitle = c.Book?.title ?? "-",
                BookPrice = c.Book?.price ?? 0,
                c.Quantity,
                Total = (c.Book?.price ?? 0) * c.Quantity,
                IsInStock = c.Book?.bookquantity >= c.Quantity,
                AvailableStock = c.Book?.bookquantity ?? 0
            }).ToList();

            ViewBag.EnrichedCart = enrichedCart;
            ViewBag.TotalAmount = enrichedCart.Sum(c => c.Total);
            ViewBag.ItemCount = enrichedCart.Count;

            return View();
        }

        // POST: Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int bookId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));

            // Stok kontrolü
            if (!await _inventoryService.CanAddToCart(bookId, quantity, userId))
            {
                TempData["Error"] = "Yetersiz stok veya sepetinizde bu kitaptan yeterli miktar var.";
                return RedirectToAction("catalogue", "Books");
            }

            var cartItem = await _context.Cart
                .FirstOrDefaultAsync(c => c.UserId == userId && c.BookId == bookId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                cartItem.UpdatedDate = DateTime.Now;
                _context.Update(cartItem);
            }
            else
            {
                _context.Cart.Add(new Cart
                {
                    UserId = userId,
                    BookId = bookId,
                    Quantity = quantity,
                    AddedDate = DateTime.Now,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Kitap sepete eklendi.";
            return RedirectToAction("Index");
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartId, int quantity)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            var cartItem = await _context.Cart
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cartItem == null)
            {
                TempData["Error"] = "Sepet öğesi bulunamadı.";
                return RedirectToAction("Index");
            }

            // Stok kontrolü
            if (cartItem.Book?.bookquantity < quantity)
            {
                TempData["Error"] = "Yetersiz stok.";
                return RedirectToAction("Index");
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Miktar güncellendi.";
            return RedirectToAction("Index");
        }

        // POST: Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            var cartItem = await _context.Cart.FindAsync(id);
            if (cartItem != null)
            {
                _context.Cart.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ürün sepetten kaldırıldı.";
            }

            return RedirectToAction("Index");
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));

            try
            {
                var success = await _orderService.ProcessCheckout(userId);
                if (success)
                {
                    TempData["Success"] = "Siparişiniz başarıyla oluşturuldu.";
                    return RedirectToAction("myorders", "orders");
                }
                else
                {
                    TempData["Error"] = "Sipariş oluşturulamadı. Lütfen stok durumunu kontrol edin.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sipariş işlemi sırasında bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        // GET: Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            var cartItems = await _context.Cart.Where(c => c.UserId == userId).ToListAsync();
            
            _context.Cart.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sepet temizlendi.";
            return RedirectToAction("Index");
        }
    }
} 