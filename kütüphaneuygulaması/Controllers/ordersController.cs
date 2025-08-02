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
    public class ordersController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly INotificationService _notificationService;

        public ordersController(kütüphaneuygulamasıContext context, IOrderService orderService, 
                             IInventoryService inventoryService, INotificationService notificationService)
        {
            _context = context;
            _orderService = orderService;
            _inventoryService = inventoryService;
            _notificationService = notificationService;
        }

        // GET: orders
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            var orders = await _orderService.GetAllOrders();
            var statusCounts = await _orderService.GetOrderStatusCounts();
            
            ViewBag.TotalSales = orders.Sum(o => o.TotalPrice);
            ViewBag.StatusCounts = statusCounts;
            ViewBag.OrderStatuses = Enum.GetValues<OrderStatus>();

            return View(orders);
        }

        // GET: orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _orderService.GetOrderById(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: orders/Create
        public async Task<IActionResult> Create(int? id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Book = book;
            ViewBag.AvailableStock = await _inventoryService.GetAvailableStock(id.Value);
            return View();
        }

        // POST: orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookId, int quantity, string? deliveryAddress, string? phoneNumber, string? notes)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            try
            {
                int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
                var order = await _orderService.CreateOrder(userId, bookId, quantity, deliveryAddress, phoneNumber, notes);

                // Bildirim gönder
                await _notificationService.SendOrderConfirmationEmail(order.Id);

                TempData["Success"] = "Siparişiniz başarıyla oluşturuldu.";
                
                if (HttpContext.Session.GetString("Role") == "admin")
                    return RedirectToAction(nameof(Index));
                else
                    return RedirectToAction("myorders");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Create", new { id = bookId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Sipariş oluşturulurken bir hata oluştu.";
                return RedirectToAction("Create", new { id = bookId });
            }
        }

        public async Task<IActionResult> myorders()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            var orders = await _orderService.GetUserOrders(userId);

            ViewBag.EnrichedOrders = orders.Select(o => new
            {
                o.Id,
                o.bookId,
                o.quantity,
                o.orderdate,
                o.Status,
                o.StatusText,
                o.StatusColor,
                BookTitle = o.Book?.title ?? "-",
                BookPrice = o.Book?.price ?? 0,
                Total = o.TotalPrice
            }).ToList();

            return View();
        }

        // GET: orders/UpdateStatus/5
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            if (id == null)
                return NotFound();

            var order = await _orderService.GetOrderById(id.Value);
            if (order == null)
                return NotFound();

            ViewBag.OrderStatuses = Enum.GetValues<OrderStatus>();
            return View(order);
        }

        // POST: orders/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            try
            {
                var success = await _orderService.UpdateOrderStatus(id, status);
                if (success)
                {
                    // Bildirim gönder
                    await _notificationService.SendOrderStatusUpdateEmail(id, status);

                    TempData["Success"] = "Sipariş durumu güncellendi.";
                }
                else
                {
                    TempData["Error"] = "Sipariş durumu güncellenemedi.";
                }
            }
            catch (Exception)
            {
                TempData["Error"] = "Sipariş durumu güncellenirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            if (id == null)
                return NotFound();

            var order = await _orderService.GetOrderById(id.Value);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,bookId,userid,quantity,orderdate,Status,TotalPrice,DeliveryAddress,PhoneNumber,Notes")] orders order)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _orderService.GetOrderById(id);
                    if (existingOrder == null)
                        return NotFound();

                    // Sadece belirli alanları güncelle
                    existingOrder.quantity = order.quantity;
                    existingOrder.Status = order.Status;
                    existingOrder.DeliveryAddress = order.DeliveryAddress;
                    existingOrder.PhoneNumber = order.PhoneNumber;
                    existingOrder.Notes = order.Notes;
                    existingOrder.UpdatedDate = DateTime.Now;

                    _context.Update(existingOrder);
                    await _context.SaveChangesAsync();

                    // Durum değiştiyse bildirim gönder
                    if (existingOrder.Status != order.Status)
                    {
                        await _notificationService.SendOrderStatusUpdateEmail(id, order.Status);
                    }

                    TempData["Success"] = "Sipariş başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await OrderExists(order.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(order);
        }

        // GET: orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            if (id == null)
                return NotFound();

            var order = await _orderService.GetOrderById(id.Value);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("myorders");

            var order = await _orderService.GetOrderById(id);
            if (order != null)
            {
                // Sipariş iptal edilirse stoku geri ver
                if (order.Status != OrderStatus.Cancelled)
                {
                    await _inventoryService.ReleaseStock(order.bookId, order.quantity);
                }

                _context.orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sipariş silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> OrderExists(int id)
        {
            return await _context.orders.AnyAsync(e => e.Id == id);
        }
    }
}
