using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

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
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        /// <summary>
        /// Dashboard özet raporu
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var report = await _reportService.GetDashboardReport(GetUserId(), startDate, endDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard raporu hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satış raporu PDF
        /// </summary>
        [HttpGet("sales-pdf")]
        public async Task<IActionResult> GetSalesReportPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateSalesReportPdf(GetUserId(), startDate, endDate);
                return File(pdfBytes, "application/pdf", $"SatisRaporu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (NotImplementedException)
            {
                return BadRequest(new { message = "PDF rapor özelliği henüz geliştirilmedi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF rapor oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok raporu Excel
        /// </summary>
        [HttpGet("stock-excel")]
        public async Task<IActionResult> GetStockReportExcel()
        {
            try
            {
                var excelBytes = await _reportService.GenerateStockReportExcel(GetUserId());
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"StokRaporu_{DateTime.Now:yyyyMMdd}.xlsx");
            }
            catch (NotImplementedException)
            {
                return BadRequest(new { message = "Excel rapor özelliği henüz geliştirilmedi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel rapor oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}