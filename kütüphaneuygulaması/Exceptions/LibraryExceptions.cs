using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Exceptions
{
    public class BookNotFoundException : Exception
    {
        public int BookId { get; }

        public BookNotFoundException(int bookId) : base($"Kitap bulunamadı. ID: {bookId}")
        {
            BookId = bookId;
        }
    }

    public class InsufficientStockException : Exception
    {
        public int BookId { get; }
        public int RequestedQuantity { get; }
        public int AvailableStock { get; }

        public InsufficientStockException(int bookId, int requestedQuantity, int availableStock) 
            : base($"Yetersiz stok. İstenen: {requestedQuantity}, Mevcut: {availableStock}")
        {
            BookId = bookId;
            RequestedQuantity = requestedQuantity;
            AvailableStock = availableStock;
        }
    }

    public class UserNotFoundException : Exception
    {
        public int UserId { get; }

        public UserNotFoundException(int userId) : base($"Kullanıcı bulunamadı. ID: {userId}")
        {
            UserId = userId;
        }
    }

    public class OrderNotFoundException : Exception
    {
        public int OrderId { get; }

        public OrderNotFoundException(int orderId) : base($"Sipariş bulunamadı. ID: {orderId}")
        {
            OrderId = orderId;
        }
    }

    public class ValidationException : Exception
    {
        public List<ValidationResult> Errors { get; }

        public ValidationException(List<ValidationResult> errors) : base("Doğrulama hatası")
        {
            Errors = errors;
        }
    }

    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message)
        {
        }
    }

    public class AuthorizationException : Exception
    {
        public string RequiredRole { get; }

        public AuthorizationException(string requiredRole) : base($"Bu işlem için {requiredRole} yetkisi gereklidir.")
        {
            RequiredRole = requiredRole;
        }
    }

    public class DatabaseConnectionException : Exception
    {
        public DatabaseConnectionException(string message) : base($"Veritabanı bağlantı hatası: {message}")
        {
        }
    }

    public class FileUploadException : Exception
    {
        public string FileName { get; }

        public FileUploadException(string fileName, string message) : base($"Dosya yükleme hatası: {message}")
        {
            FileName = fileName;
        }
    }

    public class CacheException : Exception
    {
        public string CacheKey { get; }

        public CacheException(string cacheKey, string message) : base($"Önbellek hatası: {message}")
        {
            CacheKey = cacheKey;
        }
    }

    public class SearchException : Exception
    {
        public string SearchTerm { get; }

        public SearchException(string searchTerm, string message) : base($"Arama hatası: {message}")
        {
            SearchTerm = searchTerm;
        }
    }

    public class NotificationException : Exception
    {
        public string NotificationType { get; }

        public NotificationException(string notificationType, string message) : base($"Bildirim hatası: {message}")
        {
            NotificationType = notificationType;
        }
    }

    public class BusinessRuleException : Exception
    {
        public string RuleName { get; }

        public BusinessRuleException(string ruleName, string message) : base($"İş kuralı hatası: {message}")
        {
            RuleName = ruleName;
        }
    }

    public class ConcurrencyException : Exception
    {
        public string EntityName { get; }
        public int EntityId { get; }

        public ConcurrencyException(string entityName, int entityId) 
            : base($"{entityName} (ID: {entityId}) başka bir kullanıcı tarafından güncellenmiş.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }
    }

    public class ResourceNotFoundException : Exception
    {
        public string ResourceType { get; }
        public string ResourceId { get; }

        public ResourceNotFoundException(string resourceType, string resourceId) 
            : base($"{resourceType} bulunamadı: {resourceId}")
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
        }
    }

    public class ServiceUnavailableException : Exception
    {
        public string ServiceName { get; }

        public ServiceUnavailableException(string serviceName) : base($"{serviceName} servisi şu anda kullanılamıyor.")
        {
            ServiceName = serviceName;
        }
    }
} 