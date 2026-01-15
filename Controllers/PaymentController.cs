using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] PaymentType? type = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.GetPaymentsAsync(userId, pageNumber, pageSize, type, startDate, endDate);
        return Ok(ApiResponse<PagedResult<PaymentDto>>.SuccessResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var payment = await _paymentService.GetPaymentByIdAsync(id, userId);

        if (payment == null)
        {
            return NotFound(ApiResponse<PaymentDto>.ErrorResponse("Ödeme bulunamadı"));
        }

        return Ok(ApiResponse<PaymentDto>.SuccessResponse(payment));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.CreatePaymentAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<PaymentDto>.ErrorResponse(result.Message));
        }

        return CreatedAtAction(nameof(GetPaymentById), new { id = result.Data!.Id },
            ApiResponse<PaymentDto>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] CreatePaymentRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.UpdatePaymentAsync(id, request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<PaymentDto>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<PaymentDto>.SuccessResponse(result.Data!, result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _paymentService.DeletePaymentAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}