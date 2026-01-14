using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hesapix.Services.Interfaces;
using System.Security.Claims;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IExcelService _excelService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            IPdfService pdfService,
            IExcelService excelService,
            IEmailService emailService,
            ILogger<ExportController> logger)
        {
            _pdfService = pdfService;
            _excelService = excelService;
            _emailService = emailService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        private string GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }

        private string GetUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        }

        #region PDF Exports

        /// <summary>
        /// Fatura PDF'i oluştur
        /// </summary>
        [HttpGet("pdf/invoice/{saleId}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateInvoicePdf(int saleId)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(saleId, GetUserId());

                _logger.LogInformation("Fatura PDF oluşturuldu - SaleId: {SaleId}", saleId);

                return File(pdfBytes, "application/pdf", $"Fatura_{saleId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatura PDF oluşturma hatası - SaleId: {SaleId}", saleId);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Satış raporu PDF'i oluştur
        /// </summary>
        [HttpGet("pdf/sales-report")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateSalesReportPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateSalesReportPdfAsync(startDate, endDate, GetUserId());

                _logger.LogInformation("Satış raporu PDF oluşturuldu");

                return File(pdfBytes, "application/pdf", $"Satis_Raporu_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış raporu PDF oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stok raporu PDF'i oluştur
        /// </summary>
        [HttpGet("pdf/stock-report")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateStockReportPdf()
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateStockReportPdfAsync(GetUserId());

                _logger.LogInformation("Stok raporu PDF oluşturuldu");

                return File(pdfBytes, "application/pdf", $"Stok_Raporu_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok raporu PDF oluşturma hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Excel Exports

        /// <summary>
        /// Satışları Excel'e aktar
        /// </summary>
        [HttpGet("excel/sales")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportSalesToExcel([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var excelBytes = await _excelService.ExportSalesAsync(startDate, endDate, GetUserId());

                _logger.LogInformation("Satışlar Excel'e aktarıldı");

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Satislar_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satışlar Excel export hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Stokları Excel'e aktar
        /// </summary>
        [HttpGet("excel/stocks")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportStocksToExcel()
        {
            try
            {
                var excelBytes = await _excelService.ExportStocksAsync(GetUserId());

                _logger.LogInformation("Stoklar Excel'e aktarıldı");

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Stoklar_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stoklar Excel export hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ödemeleri Excel'e aktar
        /// </summary>
        [HttpGet("excel/payments")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportPaymentsToExcel([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var excelBytes = await _excelService.ExportPaymentsAsync(startDate, endDate, GetUserId());

                _logger.LogInformation("Ödemeler Excel'e aktarıldı");

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Odemeler_{DateTime.Now:yyyyMMdd}.xlsx"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödemeler Excel export hatası");
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion

        #region Email with Attachments

        /// <summary>
        /// Faturayı email ile gönder
        /// </summary>
        [HttpPost("email/invoice/{saleId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> EmailInvoice(int saleId, [FromBody] EmailInvoiceRequest request)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(saleId, GetUserId());

                await _emailService.SendInvoiceEmailAsync(
                    request.Email ?? GetUserEmail(),
                    request.Name ?? GetUserName(),
                    pdfBytes,
                    $"INV-{saleId}"
                );

                _logger.LogInformation("Fatura email ile gönderildi - SaleId: {SaleId}, Email: {Email}", saleId, request.Email);

                return Ok(new { message = "Fatura başarıyla email ile gönderildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatura email gönderme hatası - SaleId: {SaleId}", saleId);
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }

    public class EmailInvoiceRequest
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
    }
}