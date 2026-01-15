using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StokController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StokController> _logger;

        public StokController(IStockService stockService, ILogger<StokController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<StockDto>>>> GetStocks(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<StockDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.GetStocksByUserIdAsync(userId, search, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stoklar listelenirken hata");
                return StatusCode(500, ApiResponse<List<StockDto>>.FailResult("Stoklar listelenemedi"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StockDto>>> GetStock(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<StockDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.GetStockByIdAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok bilgisi alınırken hata: StockId={StockId}", id);
                return StatusCode(500, ApiResponse<StockDto>.FailResult("Stok bilgisi alınamadı"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<StockDto>>> CreateStock([FromBody] CreateStockRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<StockDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<StockDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.CreateStockAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Yeni stok oluşturuldu: UserId={UserId}, StockId={StockId}", userId, result.Data?.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok oluşturulurken hata");
                return StatusCode(500, ApiResponse<StockDto>.FailResult("Stok oluşturulamadı"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<StockDto>>> UpdateStock(int id, [FromBody] CreateStockRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<StockDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<StockDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.UpdateStockAsync(id, request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Stok güncellendi: UserId={UserId}, StockId={StockId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok güncellenirken hata: StockId={StockId}", id);
                return StatusCode(500, ApiResponse<StockDto>.FailResult("Stok güncellenemedi"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteStock(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<bool>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.DeleteStockAsync(id, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Stok silindi: UserId={UserId}, StockId={StockId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok silinirken hata: StockId={StockId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResult("Stok silinemedi"));
            }
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<ApiResponse<List<StockDto>>>> GetLowStockItems([FromQuery] int threshold = 10)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<StockDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _stockService.GetLowStockItemsAsync(userId, threshold);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok listelenmesinde hata");
                return StatusCode(500, ApiResponse<List<StockDto>>.FailResult("Düşük stok listelenemedi"));
            }
        }
    }

    public class CreateStockRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public string? Unit { get; set; }
        public int? MinStockLevel { get; set; }
        public string? Description { get; set; }
    }
}