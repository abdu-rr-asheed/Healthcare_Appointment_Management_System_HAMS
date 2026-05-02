using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HAMS.API.Models.DTOs.Responses;

namespace HAMS.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
            var correlationId = context.TraceIdentifier;
            
            _logger.LogError(
                exception,
                "An unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}",
                correlationId,
                context.Request.Path);

            var response = context.Response;

            response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ArgumentNullException => (400, "One or more required parameters are missing."),
                ArgumentException => (400, exception.Message),
                UnauthorizedAccessException => (401, "You are not authorized to access this resource."),
                KeyNotFoundException => (404, "The requested resource was not found."),
                InvalidOperationException => (400, exception.Message),
                TimeoutException => (408, "The request timed out. Please try again."),
                _ => (500, "An internal error occurred. Please contact support.")
            };

            response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse
            {
                Message = message,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(json);
        }
    }
}