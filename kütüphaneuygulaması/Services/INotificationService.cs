using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Services
{
    public interface INotificationService
    {
        Task SendOrderConfirmationEmail(int orderId);
        Task SendOrderStatusUpdateEmail(int orderId, OrderStatus newStatus);
        Task SendLowStockAlert(int bookId);
        Task SendWelcomeEmail(int userId);
        Task SendPasswordResetEmail(string email);
        Task<bool> SendEmail(string to, string subject, string body);
        Task<bool> SendSMS(string phoneNumber, string message);
    }
} 