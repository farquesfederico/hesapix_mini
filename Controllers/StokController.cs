using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Services.Interfaces;
using System.Security.Claims;
using Hesapix.Models.DTOs.Stock;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly ILogger<StockController> _logger;

        public StockController(IStockService stockService, ILogger<StockController> logger)
        {
            _stockService = stockService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        /// <summary>
        /// Tüm stokları getir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var stocks = await _stockService.GetAllStocks(GetUserId());
                return Ok(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// ID'ye göre stok getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var stock = await _stockService.GetStockById(id, GetUserId());
                return Ok(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok getirme hatası - ID: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ürün koduna göre stok getir
        /// </summary>
        [HttpGet("by-code/{productCode}")]
        public async Task<IActionResult> GetByCode(string productCode)
        {
            try
            {
                var stock = await _stockService.GetStockByCode(productCode, GetUserId());
                return Ok(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok getirme hatası - Code: {Code}", productCode);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Yeni stok ekle
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StockDto dto)
        {
            try
            {
                var stock = await _stockService.CreateStock(dto, GetUserId());
                _logger.LogInformation("Yeni stok eklendi - Code: {Code}", stock.ProductCode);
                return CreatedAtAction(nameof(GetById), new { id = stock.Id }, stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok ekleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok güncelle
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StockDto dto)
        {
            try
            {
                var stock = await _stockService.UpdateStock(id, dto, GetUserId());
                _logger.LogInformation("Stok güncellendi - ID: {Id}", id);
                return Ok(stock);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok güncelleme hatası - ID: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok sil
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _stockService.DeleteStock(id, GetUserId());

                if (!result)
                {
                    return NotFound(new { message = "Stok bulunamadı" });
                }

                _logger.LogInformation("Stok silindi - ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok silme hatası - ID: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok miktarı güncelle
        /// </summary>
        [HttpPatch("{id}/quantity")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var result = await _stockService.UpdateStockQuantity(id, request.Quantity, GetUserId());

                if (!result)
                {
                    return NotFound(new { message = "Stok bulunamadı" });
                }

                _logger.LogInformation("Stok miktarı güncellendi - ID: {Id}, Quantity: {Quantity}", id, request.Quantity);
                return Ok(new { message = "Stok miktarı güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok miktarı güncelleme hatası - ID: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Düşük stokları getir
        /// </summary>
        [HttpGet("low-stocks")]
        public async Task<IActionResult> GetLowStocks()
        {
            try
            {
                var stocks = await _stockService.GetLowStocks(GetUserId());
                return Ok(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok arama
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "Arama terimi boş olamaz" });
                }

                var stocks = await _stockService.SearchStocks(searchTerm, GetUserId());
                return Ok(stocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok arama hatası - Term: {SearchTerm}", searchTerm);
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class UpdateQuantityRequest
    {
        public decimal Quantity { get; set; }
    }
}