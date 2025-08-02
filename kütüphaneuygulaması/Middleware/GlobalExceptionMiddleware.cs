using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace kütüphaneuygulaması.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                TraceId = context.TraceIdentifier,
                Message = _environment.IsDevelopment() ? exception.Message : "Beklenmeyen bir hata oluştu.",
                Details = _environment.IsDevelopment() ? exception.ToString() : null,
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "Bu işlem için yetkiniz bulunmamaktadır.";
                    break;

                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = "Geçersiz parametre: " + exception.Message;
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = exception.Message;
                    break;

                case FileNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = "İstenen dosya bulunamadı.";
                    break;

                case DbUpdateException:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Veritabanı işlemi sırasında bir hata oluştu.";
                    _logger.LogError(exception, "Database error occurred");
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse.Message = "İşlem zaman aşımına uğradı.";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Beklenmeyen bir hata oluştu.";
                    break;
            }

            // Log the exception
            _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

            // Check if it's an AJAX request
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await response.WriteAsync(jsonResponse);
            }
            else
            {
                // Redirect to error page for non-AJAX requests
                context.Response.Redirect($"/Home/Error?traceId={context.TraceIdentifier}&message={Uri.EscapeDataString(errorResponse.Message)}");
            }
        }
    }

    public class ErrorResponse
    {
        public string TraceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 