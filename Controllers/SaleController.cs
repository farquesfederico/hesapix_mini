using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ILogger<SaleController> _logger;

        public SaleController(ISaleService saleService, ILogger<SaleController> logger)
        {
            _saleService = saleService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SaleDto>>>> GetSales(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<SaleDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _saleService.GetSalesByUserIdAsync(userId, startDate, endDate, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satışlar listelenirken hata");
                return StatusCode(500, ApiResponse<List<SaleDto>>.FailResult("Satışlar listelenemedi"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> GetSale(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<SaleDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _saleService.GetSaleByIdAsync(id, userId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış bilgisi alınırken hata: SaleId={SaleId}", id);
                return StatusCode(500, ApiResponse<SaleDto>.FailResult("Satış bilgisi alınamadı"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SaleDto>>> CreateSale([FromBody] CreateSaleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<SaleDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<SaleDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _saleService.CreateSaleAsync(request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Yeni satış oluşturuldu: UserId={UserId}, SaleId={SaleId}", userId, result.Data?.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış oluşturulurken hata");
                return StatusCode(500, ApiResponse<SaleDto>.FailResult("Satış oluşturulamadı"));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<SaleDto>>> UpdateSale(int id, [FromBody] CreateSaleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<SaleDto>.FailResult("Geçersiz veri"));
                }

                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<SaleDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _saleService.UpdateSaleAsync(id, request, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Satış güncellendi: UserId={UserId}, SaleId={SaleId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış güncellenirken hata: SaleId={SaleId}", id);
                return StatusCode(500, ApiResponse<SaleDto>.FailResult("Satış güncellenemedi"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSale(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<bool>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _saleService.DeleteSaleAsync(id, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Satış silindi: UserId={UserId}, SaleId={SaleId}", userId, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış silinirken hata: SaleId={SaleId}", id);
                return StatusCode(500, ApiResponse<bool>.FailResult("Satış silinemedi"));
            }
        }
    }
}