using ClosedXML.Excel;
using Hesapix.Data;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations;

public class ExcelService : IExcelService
{
    private readonly ApplicationDbContext _context;

    public ExcelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportSalesToExcelAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Sales
            .Include(s => s.SaleItems)
            .Where(s => s.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(s => s.SaleDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SaleDate <= endDate.Value);

        var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Satışlar");

        // Headers
        worksheet.Cell(1, 1).Value = "Satış No";
        worksheet.Cell(1, 2).Value = "Tarih";
        worksheet.Cell(1, 3).Value = "Müşteri";
        worksheet.Cell(1, 4).Value = "Ara Toplam";
        worksheet.Cell(1, 5).Value = "KDV";
        worksheet.Cell(1, 6).Value = "İndirim";
        worksheet.Cell(1, 7).Value = "Toplam";
        worksheet.Cell(1, 8).Value = "Ödeme Yöntemi";
        worksheet.Cell(1, 9).Value = "Durum";

        // Header style
        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        int row = 2;
        foreach (var sale in sales)
        {
            worksheet.Cell(row, 1).Value = sale.SaleNumber;
            worksheet.Cell(row, 2).Value = sale.SaleDate.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 3).Value = sale.CustomerName;
            worksheet.Cell(row, 4).Value = sale.SubTotal;
            worksheet.Cell(row, 5).Value = sale.TaxAmount;
            worksheet.Cell(row, 6).Value = sale.DiscountAmount;
            worksheet.Cell(row, 7).Value = sale.TotalAmount;
            worksheet.Cell(row, 8).Value = sale.PaymentMethod.ToString();
            worksheet.Cell(row, 9).Value = sale.PaymentStatus.ToString();
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPaymentsToExcelAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Payments.Where(p => p.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ödemeler");

        // Headers
        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Müşteri/Tedarikçi";
        worksheet.Cell(1, 3).Value = "Tip";
        worksheet.Cell(1, 4).Value = "Ödeme Yöntemi";
        worksheet.Cell(1, 5).Value = "Tutar";
        worksheet.Cell(1, 6).Value = "Açıklama";

        var headerRange = worksheet.Range(1, 1, 1, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var payment in payments)
        {
            worksheet.Cell(row, 1).Value = payment.PaymentDate.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 2).Value = payment.CustomerName;
            worksheet.Cell(row, 3).Value = payment.PaymentType.ToString();
            worksheet.Cell(row, 4).Value = payment.PaymentMethod.ToString();
            worksheet.Cell(row, 5).Value = payment.Amount;
            worksheet.Cell(row, 6).Value = payment.Description;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportStocksToExcelAsync(int userId)
    {
        var stocks = await _context.Stocks
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderBy(s => s.ProductName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Stoklar");

        // Headers
        worksheet.Cell(1, 1).Value = "Ürün Kodu";
        worksheet.Cell(1, 2).Value = "Ürün Adı";
        worksheet.Cell(1, 3).Value = "Kategori";
        worksheet.Cell(1, 4).Value = "Birim";
        worksheet.Cell(1, 5).Value = "Miktar";
        worksheet.Cell(1, 6).Value = "Min. Stok";
        worksheet.Cell(1, 7).Value = "Alış Fiyatı";
        worksheet.Cell(1, 8).Value = "Satış Fiyatı";
        worksheet.Cell(1, 9).Value = "Barkod";

        var headerRange = worksheet.Range(1, 1, 1, 9);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        int row = 2;
        foreach (var stock in stocks)
        {
            worksheet.Cell(row, 1).Value = stock.ProductCode;
            worksheet.Cell(row, 2).Value = stock.ProductName;
            worksheet.Cell(row, 3).Value = stock.Category;
            worksheet.Cell(row, 4).Value = stock.Unit;
            worksheet.Cell(row, 5).Value = stock.Quantity;
            worksheet.Cell(row, 6).Value = stock.MinimumStock;
            worksheet.Cell(row, 7).Value = stock.PurchasePrice;
            worksheet.Cell(row, 8).Value = stock.SalePrice;
            worksheet.Cell(row, 9).Value = stock.Barcode;

            // Düşük stok uyarısı
            if (stock.Quantity <= stock.MinimumStock)
            {
                worksheet.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightPink;
            }

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}