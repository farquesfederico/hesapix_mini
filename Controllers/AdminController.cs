using Hesapix.Models.Common;
using Hesapix.Models.DTOs;
using Hesapix.Models.DTOs.Subs;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAuthService authService,
            ISubscriptionService subscriptionService,
            ILogger<AdminController> logger)
        {
            _authService = authService;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _authService.GetAllUsersAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcılar listelenirken hata");
                return StatusCode(500, ApiResponse<List<UserDto>>.FailResult("Kullanıcılar listelenemedi"));
            }
        }

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int userId)
        {
            try
            {
                var result = await _authService.GetUserByIdAsync(userId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bilgisi alınırken hata: UserId={UserId}", userId);
                return StatusCode(500, ApiResponse<UserDto>.FailResult("Kullanıcı bilgisi alınamadı"));
            }
        }

        [HttpPost("users/{userId}/make-admin")]
        public async Task<ActionResult<ApiResponse<bool>>> MakeUserAdmin(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _authService.UpdateUserRoleAsync(userId, "Admin");

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogWarning("Kullanıcı admin yapıldı: UserId={UserId}, By={CurrentUserId}", userId, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı admin yapılırken hata: UserId={UserId}", userId);
                return StatusCode(500, ApiResponse<bool>.FailResult("Kullanıcı admin yapılamadı"));
            }
        }

        [HttpPost("users/{userId}/remove-admin")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveUserAdmin(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // Kendi admin yetkisini kaldıramaz
                if (userId == currentUserId)
                {
                    return BadRequest(ApiResponse<bool>.FailResult("Kendi admin yetkinizi kaldıramazsınız"));
                }

                var result = await _authService.UpdateUserRoleAsync(userId, "User");

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogWarning("Kullanıcının admin yetkisi kaldırıldı: UserId={UserId}, By={CurrentUserId}", userId, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı admin yetkisi kaldırılırken hata: UserId={UserId}", userId);
                return StatusCode(500, ApiResponse<bool>.FailResult("Kullanıcı admin yetkisi kaldırılamadı"));
            }
        }

        [HttpGet("subscriptions")]
        public async Task<ActionResult<ApiResponse<List<SubscriptionDTO>>>> GetAllSubscriptions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? status = null)
        {
            try
            {
                var result = await _subscriptionService.GetAllSubscriptionsAsync(page, pageSize, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelikler listelenirken hata");
                return StatusCode(500, ApiResponse<List<SubscriptionDTO>>.FailResult("Abonelikler listelenemedi"));
            }
        }

        [HttpGet("subscriptions/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDTO>>> GetSubscriptionById(int subscriptionId)
        {
            try
            {
                var result = await _subscriptionService.GetSubscriptionByIdAsync(subscriptionId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik bilgisi alınırken hata: SubscriptionId={SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<SubscriptionDTO>.FailResult("Abonelik bilgisi alınamadı"));
            }
        }

        [HttpGet("subscriptions/user/{userId}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDTO>>> GetUserSubscription(int userId)
        {
            try
            {
                var result = await _subscriptionService.GetUserSubscriptionAsync(userId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı aboneliği alınırken hata: UserId={UserId}", userId);
                return StatusCode(500, ApiResponse<SubscriptionDTO>.FailResult("Kullanıcı aboneliği alınamadı"));
            }
        }

        [HttpPost("subscriptions")]
        public async Task<ActionResult<ApiResponse<SubscriptionDTO>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<SubscriptionDTO>.FailResult("Geçersiz veri"));
                }

                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _subscriptionService.CreateSubscriptionAsync(request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Yeni abonelik oluşturuldu: UserId={UserId}, By={CurrentUserId}", request.UserId, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik oluşturulurken hata");
                return StatusCode(500, ApiResponse<SubscriptionDTO>.FailResult("Abonelik oluşturulamadı"));
            }
        }

        [HttpPut("subscriptions/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<SubscriptionDTO>>> UpdateSubscription(
            int subscriptionId,
            [FromBody] UpdateSubscriptionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<SubscriptionDTO>.FailResult("Geçersiz veri"));
                }

                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _subscriptionService.UpdateSubscriptionAsync(subscriptionId, request);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Abonelik güncellendi: SubscriptionId={SubscriptionId}, By={CurrentUserId}", subscriptionId, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik güncellenirken hata: SubscriptionId={SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<SubscriptionDTO>.FailResult("Abonelik güncellenemedi"));
            }
        }

        [HttpDelete("subscriptions/{subscriptionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSubscription(int subscriptionId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                var result = await _subscriptionService.DeleteSubscriptionAsync(subscriptionId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogWarning("Abonelik silindi: SubscriptionId={SubscriptionId}, By={CurrentUserId}", subscriptionId, currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonelik silinirken hata: SubscriptionId={SubscriptionId}", subscriptionId);
                return StatusCode(500, ApiResponse<bool>.FailResult("Abonelik silinemedi"));
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<AdminStatisticsDto>>> GetStatistics()
        {
            try
            {
                var result = await _subscriptionService.GetAdminStatisticsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler alınırken hata");
                return StatusCode(500, ApiResponse<AdminStatisticsDto>.FailResult("İstatistikler alınamadı"));
            }
        }
    }

    public class UpdateSubscriptionRequest
    {
        public string? PlanType { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public decimal? Price { get; set; }
    }

    public class AdminStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int CancelledSubscriptions { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, int> SubscriptionsByPlan { get; set; } = new();
    }
}