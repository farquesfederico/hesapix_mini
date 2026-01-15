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

    [HttpPost("create")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _subscriptionService.CreateSubscriptionAsync(userId, request);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<object>.SuccessResponse(result.Data!, result.Message));
    }

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

    [HttpGet("price")]
    public async Task<IActionResult> GetPrice([FromQuery] CreateSubscriptionRequest request)
    {
        var price = await _subscriptionService.GetSubscriptionPriceAsync(request);
        return Ok(ApiResponse<decimal>.SuccessResponse(price));
    }

    [HttpPost("webhook/iyzico")]
    [AllowAnonymous]
    public async Task<IActionResult> IyzicoWebhook([FromBody] string payload)
    {
        var result = await _subscriptionService.HandleIyzicoWebhookAsync(payload);
        return Ok(result);
    }
}