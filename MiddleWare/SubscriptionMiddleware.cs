using Hesapix.Models.Common;
using Hesapix.Services.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace Hesapix.Middleware;

public class SubscriptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _excludedPaths =
    {
        "/api/auth/register",
        "/api/auth/login",
        "/api/auth/verify-email",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/subscription/create",
        "/api/subscription/status",
        "/api/subscription/webhook",
        "/health",
        "/swagger"
    };

    public SubscriptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISubscriptionService subscriptionService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Excluded paths kontrolü
        if (_excludedPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        // Admin kontrolü - adminleri subscription kontrolünden muaf tut
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

        // Subscription kontrolü
        var hasActiveSubscription = await subscriptionService.HasActiveSubscriptionAsync(userId);

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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
            return;
        }

        await _next(context);
    }
}