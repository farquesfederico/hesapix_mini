using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SaleController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SaleController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.GetSalesAsync(userId, pageNumber, pageSize, startDate, endDate);
        return Ok(ApiResponse<PagedResult<SaleDto>>.SuccessResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSaleById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var sale = await _saleService.GetSaleByIdAsync(id, userId);

        if (sale == null)
        {
            return NotFound(ApiResponse<SaleDto>.ErrorResponse("Satış bulunamadı"));
        }

        return Ok(ApiResponse<SaleDto>.SuccessResponse(sale));
    }

    [HttpGet("by-number/{saleNumber}")]
    public async Task<IActionResult> GetSaleByNumber(string saleNumber)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var sale = await _saleService.GetSaleByNumberAsync(saleNumber, userId);

        if (sale == null)
        {
            return NotFound(ApiResponse<SaleDto>.ErrorResponse("Satış bulunamadı"));
        }

        return Ok(ApiResponse<SaleDto>.SuccessResponse(sale));
    }

    [HttpGet("pending-payments")]
    public async Task<IActionResult> GetPendingPaymentSales(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.GetPendingPaymentSalesAsync(userId, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<SaleDto>>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.CreateSaleAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<SaleDto>.ErrorResponse(result.Message));
        }

        return CreatedAtAction(nameof(GetSaleById), new { id = result.Data!.Id },
            ApiResponse<SaleDto>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSale(int id, [FromBody] CreateSaleRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.UpdateSaleAsync(id, request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<SaleDto>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<SaleDto>.SuccessResponse(result.Data!, result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSale(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.DeleteSaleAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelSale(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.CancelSaleAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }

    [HttpPost("{id}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _saleService.UpdateSalePaymentStatusAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}