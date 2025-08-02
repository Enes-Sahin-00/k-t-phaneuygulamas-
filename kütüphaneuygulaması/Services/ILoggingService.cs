namespace kütüphaneuygulaması.Services
{
    public interface ILoggingService
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception exception, string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogCritical(Exception exception, string message, params object[] args);
        void LogUserAction(string userId, string action, string details);
        void LogSecurityEvent(string eventType, string userId, string details);
        void LogPerformance(string operation, TimeSpan duration, string details);
        void LogDatabaseOperation(string operation, string table, int recordCount);
        void LogApiCall(string endpoint, string method, int statusCode, TimeSpan duration);
    }
} 