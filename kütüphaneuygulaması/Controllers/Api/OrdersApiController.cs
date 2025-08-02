using Microsoft.AspNetCore.Mvc;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Services;
using kütüphaneuygulaması.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrdersApiController : ControllerBase
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IOrderService _orderService;
        private readonly IInventoryService _inventoryService;
        private readonly INotificationService _notificationService;
        private readonly ICacheService _cacheService;
        private readonly ILoggingService _loggingService;
        private readonly IValidationService _validationService;

        public OrdersApiController(kütüphaneuygulamasıContext context, IOrderService orderService,
                               IInventoryService inventoryService, INotificationService notificationService,
                               ICacheService cacheService, ILoggingService loggingService,
                               IValidationService validationService)
        {
            _context = context;
            _orderService = orderService;
            _inventoryService = inventoryService;
            _notificationService = notificationService;
            _cacheService = cacheService;
            _loggingService = loggingService;
            _validationService = validationService;
        }

        /// <summary>
        /// Tüm siparişleri listeler (Admin)
        /// </summary>
        /// <param name="page">Sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
        /// <param name="status">Sipariş durumu</param>
        /// <returns>Sipariş listesi</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<orders>>>> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] OrderStatus? status = null)
        {
            try
            {
                // Admin kontrolü
                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "admin")
                {
                    return Unauthorized(new ApiResponse<List<orders>>
                    {
                        Success = false,
                        Message = "Bu işlem için admin yetkisi gereklidir"
                    });
                }

                var cacheKey = $"api:orders:page:{page}:size:{pageSize}:status:{status}";
                var orders = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    var allOrders = await _orderService.GetAllOrders();
                    if (status.HasValue)
                    {
                        allOrders = allOrders.Where(o => o.Status == status.Value).ToList();
                    }
                    return allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }, TimeSpan.FromMinutes(5));

                _loggingService.LogApiCall("/api/orders", "GET", 200, TimeSpan.Zero);
                return Ok(new ApiResponse<List<orders>>
                {
                    Success = true,
                    Data = orders,
                    Message = "Siparişler başarıyla getirildi"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error getting orders from API");
                return BadRequest(new ApiResponse<List<orders>>
                {
                    Success = false,
                    Message = "Siparişler getirilirken hata oluştu"
                });
            }
        }

        /// <summary>
        /// Belirli bir siparişi getirir
        /// </summary>
        /// <param name="id">Sipariş ID'si</param>
        /// <returns>Sipariş detayları</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<orders>>> GetOrder(int id)
        {
            try
            {
                var cacheKey = $"api:order:{id}";
                var order = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    return await _orderService.GetOrderById(id);
                }, TimeSpan.FromMinutes(5));

                if (order == null)
                {
                    return NotFound(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı"
                    });
                }

                // Kullanıcı kontrolü - sadece kendi siparişini görebilir
                var userId = HttpContext.Session.GetString("userid");
                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "admin" && order.userid.ToString() != userId)
                {
                    return Unauthorized(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Bu siparişe erişim yetkiniz bulunmamaktadır"
                    });
                }

                _loggingService.LogApiCall($"/api/orders/{id}", "GET", 200, TimeSpan.Zero);
                return Ok(new ApiResponse<orders>
                {
                    Success = true,
                    Data = order,
                    Message = "Sipariş başarıyla getirildi"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error getting order from API. ID: {OrderId}", id);
                return BadRequest(new ApiResponse<orders>
                {
                    Success = false,
                    Message = "Sipariş getirilirken hata oluştu"
                });
            }
        }

        /// <summary>
        /// Yeni sipariş oluşturur
        /// </summary>
        /// <param name="orderDto">Sipariş bilgileri</param>
        /// <returns>Oluşturulan sipariş</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<orders>>> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            try
            {
                // Validation
                var validationErrors = await _validationService.ValidateModelAsync(orderDto);
                if (validationErrors.Any())
                {
                    return BadRequest(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Doğrulama hatası",
                        Errors = validationErrors.Select(e => e.ErrorMessage).ToList()
                    });
                }

                // Stok kontrolü
                if (!await _inventoryService.IsBookInStock(orderDto.BookId, orderDto.Quantity))
                {
                    return BadRequest(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Yetersiz stok"
                    });
                }

                var userId = HttpContext.Session.GetString("userid");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Giriş yapmanız gereklidir"
                    });
                }

                var order = await _orderService.CreateOrder(
                    int.Parse(userId),
                    orderDto.BookId,
                    orderDto.Quantity,
                    orderDto.DeliveryAddress,
                    orderDto.PhoneNumber,
                    orderDto.Notes
                );

                // Bildirim gönder
                await _notificationService.SendOrderConfirmationEmail(order.Id);

                // Cache temizle
                await _cacheService.InvalidateUserAsync(int.Parse(userId));

                _loggingService.LogApiCall("/api/orders", "POST", 201, TimeSpan.Zero);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new ApiResponse<orders>
                {
                    Success = true,
                    Data = order,
                    Message = "Sipariş başarıyla oluşturuldu"
                });
            }
            catch (InsufficientStockException ex)
            {
                return BadRequest(new ApiResponse<orders>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error creating order via API");
                return BadRequest(new ApiResponse<orders>
                {
                    Success = false,
                    Message = "Sipariş oluşturulurken hata oluştu"
                });
            }
        }

        /// <summary>
        /// Sipariş durumunu günceller (Admin)
        /// </summary>
        /// <param name="id">Sipariş ID'si</param>
        /// <param name="statusDto">Yeni durum</param>
        /// <returns>Güncellenen sipariş</returns>
        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<orders>>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto statusDto)
        {
            try
            {
                // Admin kontrolü
                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "admin")
                {
                    return Unauthorized(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Bu işlem için admin yetkisi gereklidir"
                    });
                }

                var order = await _orderService.GetOrderById(id);
                if (order == null)
                {
                    return NotFound(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı"
                    });
                }

                var success = await _orderService.UpdateOrderStatus(id, statusDto.Status);
                if (!success)
                {
                    return BadRequest(new ApiResponse<orders>
                    {
                        Success = false,
                        Message = "Sipariş durumu güncellenemedi"
                    });
                }

                // Bildirim gönder
                await _notificationService.SendOrderStatusUpdateEmail(id, statusDto.Status);

                // Cache temizle
                await _cacheService.RemoveAsync($"api:order:{id}");
                await _cacheService.InvalidateUserAsync(order.userid);

                _loggingService.LogApiCall($"/api/orders/{id}/status", "PUT", 200, TimeSpan.Zero);
                return Ok(new ApiResponse<orders>
                {
                    Success = true,
                    Data = order,
                    Message = "Sipariş durumu başarıyla güncellendi"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error updating order status via API. ID: {OrderId}", id);
                return BadRequest(new ApiResponse<orders>
                {
                    Success = false,
                    Message = "Sipariş durumu güncellenirken hata oluştu"
                });
            }
        }

        /// <summary>
        /// Kullanıcının siparişlerini getirir
        /// </summary>
        /// <param name="page">Sayfa numarası</param>
        /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
        /// <returns>Kullanıcının siparişleri</returns>
        [HttpGet("my-orders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<orders>>>> GetMyOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = HttpContext.Session.GetString("userid");
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<List<orders>>
                    {
                        Success = false,
                        Message = "Giriş yapmanız gereklidir"
                    });
                }

                var cacheKey = $"api:my-orders:{userId}:page:{page}:size:{pageSize}";
                var orders = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    var allOrders = await _orderService.GetUserOrders(int.Parse(userId));
                    return allOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }, TimeSpan.FromMinutes(5));

                _loggingService.LogApiCall("/api/orders/my-orders", "GET", 200, TimeSpan.Zero);
                return Ok(new ApiResponse<List<orders>>
                {
                    Success = true,
                    Data = orders,
                    Message = "Siparişleriniz başarıyla getirildi"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error getting user orders from API");
                return BadRequest(new ApiResponse<List<orders>>
                {
                    Success = false,
                    Message = "Siparişleriniz getirilirken hata oluştu"
                });
            }
        }

        /// <summary>
        /// Sipariş istatistiklerini getirir (Admin)
        /// </summary>
        /// <returns>Sipariş istatistikleri</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<object>>> GetOrderStatistics()
        {
            try
            {
                // Admin kontrolü
                var userRole = HttpContext.Session.GetString("Role");
                if (userRole != "admin")
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Bu işlem için admin yetkisi gereklidir"
                    });
                }

                var cacheKey = "api:order-statistics";
                var statistics = await _cacheService.GetOrSetAsync(cacheKey, async () =>
                {
                    var statusCounts = await _orderService.GetOrderStatusCounts();
                    var totalOrders = await _context.orders.CountAsync();
                    var totalSales = await _context.orders.SumAsync(o => o.TotalPrice);
                    var averageOrderValue = await _context.orders.AverageAsync(o => o.TotalPrice);

                    return new
                    {
                        TotalOrders = totalOrders,
                        TotalSales = totalSales,
                        AverageOrderValue = averageOrderValue,
                        StatusCounts = statusCounts
                    };
                }, TimeSpan.FromMinutes(10));

                _loggingService.LogApiCall("/api/orders/statistics", "GET", 200, TimeSpan.Zero);
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = statistics,
                    Message = "Sipariş istatistikleri başarıyla getirildi"
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(ex, "Error getting order statistics from API");
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Sipariş istatistikleri getirilirken hata oluştu"
                });
            }
        }
    }

    // DTOs
    public class CreateOrderDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BookId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }
    }
} 