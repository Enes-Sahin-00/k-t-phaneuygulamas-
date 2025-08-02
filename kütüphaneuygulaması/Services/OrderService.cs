using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Services
{
    public class OrderService : IOrderService
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IInventoryService _inventoryService;

        public OrderService(kütüphaneuygulamasıContext context, IInventoryService inventoryService)
        {
            _context = context;
            _inventoryService = inventoryService;
        }

        public async Task<orders> CreateOrder(int userId, int bookId, int quantity, string? deliveryAddress = null, string? phoneNumber = null, string? notes = null)
        {
            // Stok kontrolü
            if (!await _inventoryService.IsBookInStock(bookId, quantity))
                throw new InvalidOperationException("Yetersiz stok");

            var book = await _context.Book.FindAsync(bookId);
            if (book == null)
                throw new InvalidOperationException("Kitap bulunamadı");

            var order = new orders
            {
                bookId = bookId,
                userid = userId,
                quantity = quantity,
                orderdate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalPrice = book.price * quantity,
                DeliveryAddress = deliveryAddress,
                PhoneNumber = phoneNumber,
                Notes = notes,
                CreatedDate = DateTime.Now
            };

            _context.orders.Add(order);
            await _context.SaveChangesAsync();

            // Stoku rezerve et
            await _inventoryService.ReserveStock(bookId, quantity);

            return order;
        }

        public async Task<bool> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.Status = status;
            order.UpdatedDate = DateTime.Now;

            // Eğer sipariş iptal edilirse stoku geri ver
            if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                await _inventoryService.ReleaseStock(order.bookId, order.quantity);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<orders>> GetUserOrders(int userId)
        {
            return await _context.orders
                .Where(o => o.userid == userId)
                .Include(o => o.Book)
                .Include(o => o.User)
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        public async Task<List<orders>> GetAllOrders()
        {
            return await _context.orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        public async Task<orders?> GetOrderById(int orderId)
        {
            return await _context.orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<decimal> CalculateOrderTotal(int bookId, int quantity)
        {
            var book = await _context.Book.FindAsync(bookId);
            return book?.price * quantity ?? 0;
        }

        public async Task<bool> ProcessCheckout(int userId)
        {
            var cartItems = await _context.Cart
                .Where(c => c.UserId == userId)
                .Include(c => c.Book)
                .ToListAsync();

            if (!cartItems.Any())
                return false;

            var orders = new List<orders>();

            foreach (var cartItem in cartItems)
            {
                // Stok kontrolü
                if (!await _inventoryService.IsBookInStock(cartItem.BookId, cartItem.Quantity))
                {
                    // Yetersiz stok varsa işlemi iptal et
                    return false;
                }

                var order = new orders
                {
                    bookId = cartItem.BookId,
                    userid = userId,
                    quantity = cartItem.Quantity,
                    orderdate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalPrice = cartItem.Book.price * cartItem.Quantity,
                    CreatedDate = DateTime.Now
                };

                orders.Add(order);
            }

            // Tüm siparişleri ekle
            _context.orders.AddRange(orders);

            // Sepeti temizle
            _context.Cart.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            // Stokları rezerve et
            foreach (var order in orders)
            {
                await _inventoryService.ReserveStock(order.bookId, order.quantity);
            }

            return true;
        }

        public async Task<List<orders>> GetOrdersByStatus(OrderStatus status)
        {
            return await _context.orders
                .Where(o => o.Status == status)
                .Include(o => o.Book)
                .Include(o => o.User)
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        public async Task<Dictionary<OrderStatus, int>> GetOrderStatusCounts()
        {
            var counts = await _context.orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<OrderStatus, int>();
            foreach (var status in Enum.GetValues<OrderStatus>())
            {
                result[status] = counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;
            }

            return result;
        }
    }
} 