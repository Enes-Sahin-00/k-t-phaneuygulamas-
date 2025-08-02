using Microsoft.Extensions.Logging;

namespace kütüphaneuygulaması.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            _logger.LogError(exception, message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            _logger.LogCritical(exception, message, args);
        }

        public void LogUserAction(string userId, string action, string details)
        {
            _logger.LogInformation("User Action - UserId: {UserId}, Action: {Action}, Details: {Details}", 
                userId, action, details);
        }

        public void LogSecurityEvent(string eventType, string userId, string details)
        {
            _logger.LogWarning("Security Event - Type: {EventType}, UserId: {UserId}, Details: {Details}", 
                eventType, userId, details);
        }

        public void LogPerformance(string operation, TimeSpan duration, string details)
        {
            if (duration.TotalMilliseconds > 1000) // Log slow operations
            {
                _logger.LogWarning("Slow Operation - {Operation}: {Duration}ms, Details: {Details}", 
                    operation, duration.TotalMilliseconds, details);
            }
            else
            {
                _logger.LogDebug("Performance - {Operation}: {Duration}ms, Details: {Details}", 
                    operation, duration.TotalMilliseconds, details);
            }
        }

        public void LogDatabaseOperation(string operation, string table, int recordCount)
        {
            _logger.LogInformation("Database Operation - {Operation} on {Table}, Records: {RecordCount}", 
                operation, table, recordCount);
        }

        public void LogApiCall(string endpoint, string method, int statusCode, TimeSpan duration)
        {
            var logLevel = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, "API Call - {Method} {Endpoint}, Status: {StatusCode}, Duration: {Duration}ms", 
                method, endpoint, statusCode, duration.TotalMilliseconds);
        }
    }
} 