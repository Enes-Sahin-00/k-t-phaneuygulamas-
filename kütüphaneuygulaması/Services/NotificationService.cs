using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;
using Microsoft.EntityFrameworkCore;

namespace kütüphaneuygulaması.Services
{
    public class NotificationService : INotificationService
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(kütüphaneuygulamasıContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendOrderConfirmationEmail(int orderId)
        {
            try
            {
                var order = await _context.orders
                    .Include(o => o.Book)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return;

                var subject = "Sipariş Onayı";
                var body = $@"
                    <h2>Siparişiniz Alındı!</h2>
                    <p>Sayın {order.User?.name},</p>
                    <p>Siparişiniz başarıyla alındı.</p>
                    <h3>Sipariş Detayları:</h3>
                    <ul>
                        <li><strong>Kitap:</strong> {order.Book?.title}</li>
                        <li><strong>Miktar:</strong> {order.quantity}</li>
                        <li><strong>Toplam:</strong> {order.TotalPrice:C}</li>
                        <li><strong>Sipariş Tarihi:</strong> {order.orderdate:dd.MM.yyyy HH:mm}</li>
                    </ul>
                    <p>Sipariş durumunuzu takip etmek için hesabınıza giriş yapabilirsiniz.</p>";

                await SendEmail(order.User?.email ?? "", subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş onay emaili gönderilemedi. OrderId: {OrderId}", orderId);
            }
        }

        public async Task SendOrderStatusUpdateEmail(int orderId, OrderStatus newStatus)
        {
            try
            {
                var order = await _context.orders
                    .Include(o => o.Book)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return;

                var statusText = newStatus switch
                {
                    OrderStatus.Confirmed => "Onaylandı",
                    OrderStatus.Shipped => "Kargoya Verildi",
                    OrderStatus.Delivered => "Teslim Edildi",
                    OrderStatus.Cancelled => "İptal Edildi",
                    _ => "Güncellendi"
                };

                var subject = $"Sipariş Durumu Güncellendi - {statusText}";
                var body = $@"
                    <h2>Sipariş Durumu Güncellendi</h2>
                    <p>Sayın {order.User?.name},</p>
                    <p>Siparişinizin durumu güncellendi.</p>
                    <h3>Sipariş Detayları:</h3>
                    <ul>
                        <li><strong>Kitap:</strong> {order.Book?.title}</li>
                        <li><strong>Miktar:</strong> {order.quantity}</li>
                        <li><strong>Yeni Durum:</strong> {statusText}</li>
                        <li><strong>Güncelleme Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</li>
                    </ul>";

                await SendEmail(order.User?.email ?? "", subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş durumu güncelleme emaili gönderilemedi. OrderId: {OrderId}", orderId);
            }
        }

        public async Task SendLowStockAlert(int bookId)
        {
            try
            {
                var book = await _context.Book.FindAsync(bookId);
                if (book == null) return;

                var subject = "Düşük Stok Uyarısı";
                var body = $@"
                    <h2>Düşük Stok Uyarısı</h2>
                    <p>Aşağıdaki kitabın stoku kritik seviyeye düştü:</p>
                    <ul>
                        <li><strong>Kitap:</strong> {book.title}</li>
                        <li><strong>Yazar:</strong> {book.author}</li>
                        <li><strong>Mevcut Stok:</strong> {book.bookquantity}</li>
                    </ul>
                    <p>Lütfen stok takviyesi yapın.</p>";

                // Admin kullanıcılarına bildirim gönder
                var adminUsers = await _context.usersaccounts
                    .Where(u => u.role == "admin" && u.IsActive)
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    await SendEmail(admin.email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok uyarısı gönderilemedi. BookId: {BookId}", bookId);
            }
        }

        public async Task SendWelcomeEmail(int userId)
        {
            try
            {
                var user = await _context.usersaccounts.FindAsync(userId);
                if (user == null) return;

                var subject = "Hoş Geldiniz!";
                var body = $@"
                    <h2>Hoş Geldiniz!</h2>
                    <p>Sayın {user.name},</p>
                    <p>Kütüphane uygulamamıza hoş geldiniz!</p>
                    <p>Artık binlerce kitap arasından seçim yapabilir ve sipariş verebilirsiniz.</p>
                    <p>Herhangi bir sorunuz olursa bizimle iletişime geçebilirsiniz.</p>";

                await SendEmail(user.email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hoş geldin emaili gönderilemedi. UserId: {UserId}", userId);
            }
        }

        public async Task SendPasswordResetEmail(string email)
        {
            try
            {
                var subject = "Şifre Sıfırlama";
                var body = $@"
                    <h2>Şifre Sıfırlama</h2>
                    <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
                    <p><a href='#'>Şifremi Sıfırla</a></p>
                    <p>Bu işlemi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.</p>";

                await SendEmail(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şifre sıfırlama emaili gönderilemedi. Email: {Email}", email);
            }
        }

        public async Task<bool> SendEmail(string to, string subject, string body)
        {
            try
            {
                // Gerçek email gönderimi için SMTP konfigürasyonu gerekli
                // Şimdilik sadece log yazıyoruz
                _logger.LogInformation("Email gönderildi: {To}, {Subject}", to, subject);
                
                // Gerçek implementasyonda SMTP client kullanılacak
                // await _smtpClient.SendMailAsync(message);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilemedi: {To}", to);
                return false;
            }
        }

        public async Task<bool> SendSMS(string phoneNumber, string message)
        {
            try
            {
                // Gerçek SMS gönderimi için SMS provider konfigürasyonu gerekli
                _logger.LogInformation("SMS gönderildi: {PhoneNumber}, {Message}", phoneNumber, message);
                
                // Gerçek implementasyonda SMS provider kullanılacak
                // await _smsProvider.SendSMSAsync(phoneNumber, message);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS gönderilemedi: {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
} 