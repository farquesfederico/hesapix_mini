using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StokController : ControllerBase
{
    private readonly IStokService _stokService;

    public StokController(IStokService stokService)
    {
        _stokService = stokService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStocks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _stokService.GetStocksAsync(userId, pageNumber, pageSize, searchTerm, lowStockOnly);
        return Ok(ApiResponse<PagedResult<StockDto>>.SuccessResponse(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStockById(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var stock = await _stokService.GetStockByIdAsync(id, userId);

        if (stock == null)
        {
            return NotFound(ApiResponse<StockDto>.ErrorResponse("Stok bulunamadı"));
        }

        return Ok(ApiResponse<StockDto>.SuccessResponse(stock));
    }

    [HttpPost]
    public async Task<IActionResult> CreateStock([FromBody] CreateStockRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _stokService.CreateStockAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<StockDto>.ErrorResponse(result.Message));
        }

        return CreatedAtAction(nameof(GetStockById), new { id = result.Data!.Id },
            ApiResponse<StockDto>.SuccessResponse(result.Data, result.Message));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _stokService.UpdateStockAsync(id, request, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<StockDto>.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse<StockDto>.SuccessResponse(result.Data!, result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStock(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _stokService.DeleteStockAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.ErrorResponse(result.Message));
        }

        return Ok(ApiResponse.SuccessResponse(result.Message));
    }
}