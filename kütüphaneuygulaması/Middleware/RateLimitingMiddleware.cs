using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Concurrent;

namespace kütüphaneuygulaması.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var endpoint = context.Request.Path.Value ?? "";

            if (!IsRateLimitExceeded(clientId, endpoint))
            {
                await _next(context);
            }
            else
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
                
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    success = false,
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = GetRetryAfterSeconds(clientId, endpoint),
                    timestamp = DateTime.UtcNow
                };

                context.Response.Headers.Add("Retry-After", GetRetryAfterSeconds(clientId, endpoint).ToString());
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }

        private string GetClientId(HttpContext context)
        {
            // IP adresi ve User-Agent kombinasyonu
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            return $"{ip}:{userAgent}";
        }

        private bool IsRateLimitExceeded(string clientId, string endpoint)
        {
            var key = $"{clientId}:{endpoint}";
            var now = DateTime.UtcNow;

            if (_rateLimitStore.TryGetValue(key, out var rateLimitInfo))
            {
                // Eski kayıtları temizle
                rateLimitInfo.Requests.RemoveAll(r => r < now.AddMinutes(-1));

                // Rate limit kontrolü
                var limit = GetRateLimit(endpoint);
                if (rateLimitInfo.Requests.Count >= limit)
                {
                    return true;
                }

                // Yeni istek ekle
                rateLimitInfo.Requests.Add(now);
            }
            else
            {
                // Yeni rate limit kaydı oluştur
                _rateLimitStore[key] = new RateLimitInfo
                {
                    Requests = new List<DateTime> { now }
                };
            }

            return false;
        }

        private int GetRateLimit(string endpoint)
        {
            // Endpoint bazında rate limit
            return endpoint switch
            {
                var e when e.StartsWith("/api/books/search") => 30, // Arama: 30 istek/dakika
                var e when e.StartsWith("/api/books") => 100, // Kitap listesi: 100 istek/dakika
                var e when e.StartsWith("/api/orders") => 20, // Siparişler: 20 istek/dakika
                var e when e.StartsWith("/api/admin") => 50, // Admin: 50 istek/dakika
                _ => 60 // Varsayılan: 60 istek/dakika
            };
        }

        private int GetRetryAfterSeconds(string clientId, string endpoint)
        {
            var key = $"{clientId}:{endpoint}";
            if (_rateLimitStore.TryGetValue(key, out var rateLimitInfo))
            {
                var oldestRequest = rateLimitInfo.Requests.Min();
                var timeUntilReset = 60 - (int)(DateTime.UtcNow - oldestRequest).TotalSeconds;
                return Math.Max(1, timeUntilReset);
            }
            return 60;
        }

        private class RateLimitInfo
        {
            public List<DateTime> Requests { get; set; } = new List<DateTime>();
        }
    }
} 