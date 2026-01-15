using Hesapix.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hesapix.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExcelService _excelService;
    private readonly IPdfService _pdfService;

    public ExportController(IExcelService excelService, IPdfService pdfService)
    {
        _excelService = excelService;
        _pdfService = pdfService;
    }

    [HttpGet("sales/excel")]
    public async Task<IActionResult> ExportSalesToExcel(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var excelData = await _excelService.ExportSalesToExcelAsync(userId, startDate, endDate);

        return File(excelData,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Satislar_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("payments/excel")]
    public async Task<IActionResult> ExportPaymentsToExcel(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var excelData = await _excelService.ExportPaymentsToExcelAsync(userId, startDate, endDate);

        return File(excelData,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Odemeler_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("stocks/excel")]
    public async Task<IActionResult> ExportStocksToExcel()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var excelData = await _excelService.ExportStocksToExcelAsync(userId);

        return File(excelData,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Stoklar_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("sale/{saleId}/invoice/pdf")]
    public async Task<IActionResult> ExportSaleInvoiceToPdf(int saleId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var pdfData = await _pdfService.GenerateSaleInvoicePdfAsync(saleId, userId);

        return File(pdfData, "application/pdf", $"Fatura_{saleId}.pdf");
    }
}