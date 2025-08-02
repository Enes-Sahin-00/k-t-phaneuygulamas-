using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace kütüphaneuygulaması.Middleware
{
    public class ApiAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiAuthenticationMiddleware> _logger;

        public ApiAuthenticationMiddleware(RequestDelegate next, ILogger<ApiAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // API endpoint'leri için authentication kontrolü
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Session kontrolü
                var userId = context.Session.GetString("userid");
                var userRole = context.Session.GetString("Role");

                // Public endpoints (authentication gerektirmeyen)
                var publicEndpoints = new[]
                {
                    "/api/books",
                    "/api/books/search",
                    "/api/books/popular",
                    "/api/categories"
                };

                var isPublicEndpoint = publicEndpoints.Any(endpoint => 
                    context.Request.Path.StartsWithSegments(endpoint) && 
                    context.Request.Method == "GET");

                if (!isPublicEndpoint && string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized API access attempt: {Path}", context.Request.Path);
                    
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Authentication required",
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Admin-only endpoints kontrolü
                var adminEndpoints = new[]
                {
                    "/api/orders",
                    "/api/orders/statistics",
                    "/api/users",
                    "/api/admin"
                };

                var isAdminEndpoint = adminEndpoints.Any(endpoint => 
                    context.Request.Path.StartsWithSegments(endpoint));

                if (isAdminEndpoint && userRole != "admin")
                {
                    _logger.LogWarning("Unauthorized admin API access attempt: {Path} by user {UserId}", 
                        context.Request.Path, userId);
                    
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Admin access required",
                        timestamp = DateTime.UtcNow
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }

            await _next(context);
        }
    }
} 