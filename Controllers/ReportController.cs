using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Report;
using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<DashboardReportDto>>> GetDashboardReport(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<DashboardReportDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _reportService.GetDashboardReportAsync(userId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard raporu alınırken hata");
                return StatusCode(500, ApiResponse<DashboardReportDto>.FailResult("Dashboard raporu alınamadı"));
            }
        }

        [HttpGet("sales-summary")]
        public async Task<ActionResult<ApiResponse<SalesSummaryDto>>> GetSalesSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<SalesSummaryDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _reportService.GetSalesSummaryAsync(userId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış özeti alınırken hata");
                return StatusCode(500, ApiResponse<SalesSummaryDto>.FailResult("Satış özeti alınamadı"));
            }
        }

        [HttpGet("top-products")]
        public async Task<ActionResult<ApiResponse<List<TopProductDto>>>> GetTopProducts(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int top = 10)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<List<TopProductDto>>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _reportService.GetTopProductsAsync(userId, startDate, endDate, top);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En çok satan ürünler alınırken hata");
                return StatusCode(500, ApiResponse<List<TopProductDto>>.FailResult("En çok satan ürünler alınamadı"));
            }
        }

        [HttpGet("payment-summary")]
        public async Task<ActionResult<ApiResponse<PaymentSummaryDto>>> GetPaymentSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized(ApiResponse<PaymentSummaryDto>.FailResult("Geçersiz kullanıcı"));
                }

                var result = await _reportService.GetPaymentSummaryAsync(userId, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme özeti alınırken hata");
                return StatusCode(500, ApiResponse<PaymentSummaryDto>.FailResult("Ödeme özeti alınamadı"));
            }
        }
    }

    public class SalesSummaryDto
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSaleValue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PaymentSummaryDto
    {
        public int TotalPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<string, decimal> PaymentsByMethod { get; set; } = new();
    }
}