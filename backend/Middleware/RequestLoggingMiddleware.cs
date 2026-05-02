using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HAMS.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private static readonly HashSet<string> _excludePaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/metrics",
            "/hangfire"
        };

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkipLogging(context.Request))
            {
                await _next(context);
                return;
            }

            var correlationId = context.TraceIdentifier;
            var startTime = Stopwatch.GetTimestamp();
            var requestBody = string.Empty;

            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);
                
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var userId = context.User?.FindFirst("sub")?.Value ?? "Anonymous";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger.LogInformation(
                "HTTP Request: {Method} {Path}{QueryString} | User: {UserId} | IP: {IpAddress} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                userId,
                ipAddress,
                correlationId);

            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 1000)
            {
                _logger.LogDebug(
                    "Request Body: {Body}",
                    SanitizeBody(requestBody));
            }

            var originalBodyStream = context.Response.Body;

            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context);
            }
            finally
            {
                var elapsed = Stopwatch.GetTimestamp() - startTime;

                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);

                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= 500 ? LogLevel.Error 
                    : statusCode >= 400 ? LogLevel.Warning 
                    : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    elapsed,
                    correlationId);
            }
        }

        private bool ShouldSkipLogging(HttpRequest request)
        {
            var path = request.Path.Value?.ToLower() ?? "";
            return _excludePaths.Any(excluded => path.StartsWith(excluded));
        }

        private string SanitizeBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return body;

            try
            {
                using var doc = JsonDocument.Parse(body);
                var sanitized = new Dictionary<string, object>();
                
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var key = prop.Name.ToLower();
                    if (key.Contains("password") || key.Contains("token") || key.Contains("secret"))
                    {
                        sanitized[prop.Name] = "***REDACTED***";
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        sanitized[prop.Name] = prop.Value.GetString() ?? "";
                    }
                    else
                    {
                        sanitized[prop.Name] = prop.Value.ToString();
                    }
                }

                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                return body.Length > 100 ? body.Substring(0, 100) + "..." : body;
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}