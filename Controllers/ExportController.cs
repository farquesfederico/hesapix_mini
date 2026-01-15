using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hesapix.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly IExcelService _excelService;
        private readonly IPdfService _pdfService;
        private readonly ISaleService _saleService;
        private readonly IPaymentService _paymentService;
        private readonly IStockService _stockService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            IExcelService excelService,
            IPdfService pdfService,
            ISaleService saleService,
            IPaymentService paymentService,
            IStockService stockService,
            ILogger<ExportController> logger)
        {
            _excelService = excelService;
            _pdfService = pdfService;
            _saleService = saleService;
            _paymentService = paymentService;
            _stockService = stockService;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst("UserId")?.Value ?? "0");
        }

        [HttpGet("sales/excel")]
        public async Task<IActionResult> ExportSalesToExcel(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var salesResult = await _saleService.GetSalesByUserIdAsync(userId, startDate, endDate, 1, int.MaxValue);

                if (!salesResult.Success || salesResult.Data == null)
                {
                    return BadRequest("Satış verileri alınamadı");
                }

                var excelData = _excelService.ExportSalesToExcel(salesResult.Data);

                var fileName = $"Satislar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satışlar Excel'e aktarılırken hata");
                return StatusCode(500, "Excel oluşturulamadı");
            }
        }

        [HttpGet("sales/{id}/pdf")]
        public async Task<IActionResult> ExportSaleToPdf(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var saleResult = await _saleService.GetSaleByIdAsync(id, userId);

                if (!saleResult.Success || saleResult.Data == null)
                {
                    return NotFound("Satış bulunamadı");
                }

                var pdfData = _pdfService.GenerateSaleInvoicePdf(saleResult.Data);

                var fileName = $"Fatura_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfData, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış PDF'e aktarılırken hata: SaleId={SaleId}", id);
                return StatusCode(500, "PDF oluşturulamadı");
            }
        }

        [HttpGet("payments/excel")]
        public async Task<IActionResult> ExportPaymentsToExcel(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var paymentsResult = await _paymentService.GetPaymentsByUserIdAsync(userId, startDate, endDate, 1, int.MaxValue);

                if (!paymentsResult.Success || paymentsResult.Data == null)
                {
                    return BadRequest("Ödeme verileri alınamadı");
                }

                var excelData = _excelService.ExportPaymentsToExcel(paymentsResult.Data);

                var fileName = $"Odemeler_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödemeler Excel'e aktarılırken hata");
                return StatusCode(500, "Excel oluşturulamadı");
            }
        }

        [HttpGet("stocks/excel")]
        public async Task<IActionResult> ExportStocksToExcel([FromQuery] string? search = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    return Unauthorized();
                }

                var stocksResult = await _stockService.GetStocksByUserIdAsync(userId, search, 1, int.MaxValue);

                if (!stocksResult.Success || stocksResult.Data == null)
                {
                    return BadRequest("Stok verileri alınamadı");
                }

                var excelData = _excelService.ExportStocksToExcel(stocksResult.Data);

                var fileName = $"Stoklar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stoklar Excel'e aktarılırken hata");
                return StatusCode(500, "Excel oluşturulamadı");
            }
        }
    }
}