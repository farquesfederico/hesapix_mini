using Hesapix.Models.Common;
using Hesapix.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.Json;

namespace Hesapix.Middleware;

public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private static readonly string[] ExcludedPaths =
    {
        "/api/auth/register",
        "/api/auth/login",
        "/api/auth/verify-email",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/refresh-token",
        "/api/subscription/initiate-payment",
        "/api/subscription/status",
        "/api/subscription/plans",
        "/api/subscription/webhook",
        "/api/subscription/callback",
        "/api/subscription/activate-trial",
        "/health",
        "/swagger"
    };

    public SubscriptionMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Excluded paths kontrolü
        if (ExcludedPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        // Static files ve swagger
        if (path.StartsWith("/swagger") ||
            path.Contains(".") ||
            path == "/")
        {
            await _next(context);
            return;
        }

        // Authentication kontrolü
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Admin kontrolü - adminler subscription kontrolünden muaf
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim == "Admin")
        {
            await _next(context);
            return;
        }

        // User ID kontrolü
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            await _next(context);
            return;
        }

        // Cache kontrolü - 5 dakika cache
        var cacheKey = $"subscription_active_{userId}";
        if (!_cache.TryGetValue(cacheKey, out bool hasActiveSubscription))
        {
            hasActiveSubscription = await subscriptionService.HasActiveSubscriptionAsync(userId);

            // Cache'e kaydet - sadece aktif subscription durumlarını cache'le
            if (hasActiveSubscription)
            {
                _cache.Set(cacheKey, hasActiveSubscription, TimeSpan.FromMinutes(5));
            }
        }

        if (!hasActiveSubscription)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = ApiResponse.ErrorResponse(
                "Aktif aboneliğiniz bulunmamaktadır. Lütfen abonelik satın alınız.",
                new List<string> { "SUBSCRIPTION_REQUIRED" }
            );

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
            return;
        }

        await _next(context);
    }
}