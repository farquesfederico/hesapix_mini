using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    // ❌ CREATE ENDPOINT KALDIRILDI - Sadece ödeme sonrası webhook ile oluşturulabilir

    [HttpGet("status")]
    public async Task<IActionResult> GetSubscriptionStatus()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

        if (subscription == null)
        {
            return Ok(ApiResponse<SubscriptionDto?>.SuccessResponse(null, "Aktif abonelik bulunamadı"));
        }

        return Ok(ApiResponse<SubscriptionDto>.SuccessResponse(subscription));
    }

    [HttpPost("cancel")]
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

    [HttpPost("reactivate")]
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

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAvailablePlansAsync();
        return Ok(ApiResponse<object>.SuccessResponse(plans));
    }

    [HttpPost("initiate-payment")]
    public async Task<IActionResult> InitiatePayment([FromBody] CreateSubscriptionRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.InitiatePaymentAsync(userId, request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(result.Message));
        }

        // Ödeme sayfasına yönlendirilecek URL döner
        return Ok(ApiResponse<object>.SuccessResponse(result.Data!, "Ödeme başlatıldı"));
    }

    [HttpPost("webhook/iyzico")]
    [AllowAnonymous]
    public async Task<IActionResult> IyzicoWebhook([FromBody] string payload)
    {
        var result = await _subscriptionService.HandleIyzicoWebhookAsync(payload);
        return Ok(result);
    }

    // Admin için trial activation
    [HttpPost("activate-trial/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateTrial(int userId)
    {
        var result = await _subscriptionService.ActivateTrialAsync(userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}