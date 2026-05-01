using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HAMS.API.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        
        private static readonly ConcurrentDictionary<string, RateLimitEntry> _userRequests = new();
        private static readonly ConcurrentDictionary<string, RateLimitEntry> _ipRequests = new();
        
        private const int UserRequestsPerMinute = 100;
        private const int IpRequestsPerMinute = 1000;
        private const int WindowSizeInSeconds = 60;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkipRateLimiting(context.Request))
            {
                await _next(context);
                return;
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userId = context.User?.FindFirst("sub")?.Value ?? null;
            
            var userKey = userId ?? $"anon_{ipAddress}";
            var ipKey = ipAddress;

            if (!CheckRateLimit(userKey, _userRequests, UserRequestsPerMinute))
            {
                _logger.LogWarning(
                    "Rate limit exceeded for user: {UserId} | IP: {IpAddress}",
                    userId ?? "Anonymous",
                    ipAddress);

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = 60
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            if (!CheckRateLimit(ipKey, _ipRequests, IpRequestsPerMinute))
            {
                _logger.LogWarning(
                    "Rate limit exceeded for IP: {IpAddress} | User: {UserId}",
                    ipAddress,
                    userId ?? "Anonymous");

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    message = "Too many requests from this IP address. Please try again later.",
                    retryAfter = 60
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            await _next(context);
        }

        private bool CheckRateLimit(string key, ConcurrentDictionary<string, RateLimitEntry> store, int maxRequests)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-WindowSizeInSeconds);

            if (store.TryGetValue(key, out var entry))
            {
                if (entry.WindowStart < windowStart)
                {
                    entry.WindowStart = now;
                    entry.RequestCount = 1;
                    return true;
                }

                if (entry.RequestCount >= maxRequests)
                {
                    return false;
                }

                entry.RequestCount++;
                return true;
            }

            store.TryAdd(key, new RateLimitEntry
            {
                WindowStart = now,
                RequestCount = 1
            });

            return true;
        }

        private bool ShouldSkipRateLimiting(HttpRequest request)
        {
            var path = request.Path.Value?.ToLower() ?? "";
            return path.StartsWith("/health") 
                || path.StartsWith("/metrics")
                || path.StartsWith("/hangfire");
        }
    }

    public class RateLimitEntry
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}