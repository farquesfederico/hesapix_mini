using Hesapix.Models.DTOs.Subscription;
using Hesapix.Models.Common;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/v1/admin/[controller]")]
    [Authorize] // Admin token zorunlu
    public class AdminController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public AdminController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // Admin rol kontrolü
        private bool IsAdmin() => bool.Parse(User.FindFirst("IsAdmin")?.Value ?? "false");

        /// <summary>
        /// Kullanıcıya abonelik atama (admin-only)
        /// </summary>
        [HttpPost("assign-subscription")]
        public async Task<IActionResult> AssignSubscription([FromBody] CreateSubscriptionRequest request)
        {
            if (!IsAdmin())
                return Forbid("Sadece adminler işlem yapabilir");

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var subscription = await _subscriptionService.CreateSubscription(request, adminId);

            return Ok(ApiResponse<SubscriptionDto>.SuccessResponse(subscription, "Abonelik başarıyla oluşturuldu"));
        }

        /// <summary>
        /// Tüm abonelikleri listele (admin-only)
        /// </summary>
        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetAllSubscriptions()
        {
            if (!IsAdmin())
                return Forbid("Sadece adminler işlem yapabilir");

            var subscriptions = await _subscriptionService.GetAllSubscriptions();

            return Ok(ApiResponse<List<SubscriptionDto>>.SuccessResponse(subscriptions, "Tüm abonelikler listelendi"));
        }
    }
}
