using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Kullanıcının aktif abonelik durumunu getirir
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

        if (subscription == null)
        {
            return Ok(ApiResponse<object>.SuccessResponse(
                new { HasSubscription = false },
                "Aktif abonelik bulunamadı"
            ));
        }

        return Ok(ApiResponse<SubscriptionDto>.SuccessResponse(subscription));
    }

    /// <summary>
    /// Abonelik iptali - Dönem sonunda iptal olur
    /// </summary>
    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> CancelSubscription()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.CancelSubscriptionAsync(userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    /// <summary>
    /// İptal edilmiş aboneliği yeniden aktif eder
    /// </summary>
    [HttpPost("reactivate")]
    [Authorize]
    public async Task<IActionResult> ReactivateSubscription()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.ReactivateSubscriptionAsync(userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    /// <summary>
    /// Mevcut abonelik planlarını listeler
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(ApiResponse<object>.SuccessResponse(plans));
    }

    /// <summary>
    /// Ödeme işlemini başlatır (Iyzico / Google Play / App Store)
    /// </summary>
    [HttpPost("initiate-payment")]
    [Authorize]
    public async Task<IActionResult> InitiatePayment([FromBody] CreateSubscriptionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse.ErrorResponse("Geçersiz istek"));
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.InitiatePaymentAsync(userId, request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Data!, result.Message));
    }

    /// <summary>
    /// Iyzico ödeme callback endpoint
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> IyzicoCallback([FromForm] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token bulunamadı");
        }

        var result = await _subscriptionService.HandleIyzicoCallbackAsync(token);

        if (!result.Success)
        {
            // Hata sayfasına yönlendir
            return Redirect($"/payment-failed?message={Uri.EscapeDataString(result.Message)}");
        }

        // Başarı sayfasına yönlendir
        return Redirect("/payment-success");
    }

    /// <summary>
    /// Iyzico webhook endpoint
    /// </summary>
    [HttpPost("webhook/iyzico")]
    [AllowAnonymous]
    public async Task<IActionResult> IyzicoWebhook([FromBody] string payload)
    {
        var result = await _subscriptionService.HandleIyzicoWebhookAsync(payload);
        return Ok(result);
    }

    /// <summary>
    /// Admin: Kullanıcıya trial abonelik başlatır
    /// </summary>
    [HttpPost("admin/activate-trial/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateTrialForUser(int userId)
    {
        var result = await _subscriptionService.ActivateTrialAsync(userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    /// <summary>
    /// Kendi trial aboneliğini başlatır
    /// </summary>
    [HttpPost("activate-trial")]
    [Authorize]
    public async Task<IActionResult> ActivateTrial()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.ActivateTrialAsync(userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}