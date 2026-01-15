using System.Security.Claims;

namespace Hesapix.MiddleWare
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionMiddleware> _logger;

        // Abonelik gerektirmeyen endpoint'ler
        private static readonly HashSet<string> ExemptPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/refresh-token",
            "/api/auth/change-password",
            "/swagger",
            "/health"
        };

        public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Exempt paths kontrolü
            if (IsExemptPath(path))
            {
                await _next(context);
                return;
            }

            // Admin kullanıcılar için abonelik kontrolü yapma
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Admin")
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

            // Abonelik kontrolü
            var hasActiveSubscription = context.User.FindFirst("HasActiveSubscription")?.Value;

            if (hasActiveSubscription != "True")
            {
                _logger.LogWarning("Aboneliği olmayan kullanıcı erişim denemesi: UserId={UserId}, Path={Path}",
                    context.User.FindFirst("UserId")?.Value, path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Bu işlem için aktif bir aboneliğe ihtiyacınız var",
                    errorCode = "SUBSCRIPTION_REQUIRED"
                });

                return;
            }

            await _next(context);
        }

        private bool IsExemptPath(string path)
        {
            return ExemptPaths.Any(exempt => path.StartsWith(exempt, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class SubscriptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSubscriptionCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SubscriptionMiddleware>();
        }
    }
}