using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        /// <summary>
        /// Yeni satış oluştur
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequest request)
        {
            try
            {
                var sale = await _saleService.CreateSale(request, GetUserId());
                _logger.LogInformation("Yeni satış oluşturuldu - SaleNumber: {SaleNumber}", sale.SaleNumber);
                return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tüm satışları getir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var sales = await _saleService.GetSales(GetUserId(), startDate, endDate);
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// ID'ye göre satış getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var sale = await _saleService.GetSaleById(id, GetUserId());
                return Ok(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış getirme hatası - ID: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satış numarasına göre satış getir
        /// </summary>
        [HttpGet("by-number/{saleNumber}")]
        public async Task<IActionResult> GetByNumber(string saleNumber)
        {
            try
            {
                var sale = await _saleService.GetSaleByNumber(saleNumber, GetUserId());
                return Ok(sale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış getirme hatası - SaleNumber: {SaleNumber}", saleNumber);
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satış iptal et
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var result = await _saleService.CancelSale(id, GetUserId());

                if (!result)
                {
                    return NotFound(new { message = "Satış bulunamadı" });
                }

                _logger.LogInformation("Satış iptal edildi - ID: {Id}", id);
                return Ok(new { message = "Satış başarıyla iptal edildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış iptal hatası - ID: {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bekleyen ödeme olan satışları getir
        /// </summary>
        [HttpGet("pending-payments")]
        public async Task<IActionResult> GetPendingPayments()
        {
            try
            {
                var sales = await _saleService.GetPendingPaymentSales(GetUserId());
                return Ok(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen ödeme listeleme hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satış istatistikleri
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var sales = await _saleService.GetSales(GetUserId(), startDate, endDate);

                var statistics = new
                {
                    totalSales = sales.Count,
                    totalAmount = sales.Sum(s => s.TotalAmount),
                    paidSales = sales.Count(s => s.PaymentStatus == Models.Entities.PaymentStatus.Paid),
                    pendingSales = sales.Count(s => s.PaymentStatus == Models.Entities.PaymentStatus.Pending),
                    partialPaidSales = sales.Count(s => s.PaymentStatus == Models.Entities.PaymentStatus.PartialPaid),
                    cancelledSales = sales.Count(s => s.PaymentStatus == Models.Entities.PaymentStatus.Cancelled),
                    averageSaleAmount = sales.Any() ? sales.Average(s => s.TotalAmount) : 0
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış istatistikleri hatası");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}