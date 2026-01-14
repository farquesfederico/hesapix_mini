using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class ExcelService : IExcelService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ApplicationDbContext context, ILogger<ExcelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> ExportSalesAsync(DateTime startDate, DateTime endDate, int userId)
        {
            var sales = await _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.UserId == userId && s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Satışlar");

            // Header styling
            var headerRange = worksheet.Range("A1:J1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(1, 1).Value = "Fatura No";
            worksheet.Cell(1, 2).Value = "Tarih";
            worksheet.Cell(1, 3).Value = "Müşteri Adı";
            worksheet.Cell(1, 4).Value = "Telefon";
            worksheet.Cell(1, 5).Value = "Email";
            worksheet.Cell(1, 6).Value = "Ara Toplam";
            worksheet.Cell(1, 7).Value = "KDV";
            worksheet.Cell(1, 8).Value = "İndirim";
            worksheet.Cell(1, 9).Value = "Toplam";
            worksheet.Cell(1, 10).Value = "Ödeme Durumu";

            // Data
            int row = 2;
            foreach (var sale in sales)
            {
                worksheet.Cell(row, 1).Value = sale.SaleNumber;
                worksheet.Cell(row, 2).Value = sale.SaleDate.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 3).Value = sale.CustomerName;
                worksheet.Cell(row, 4).Value = sale.CustomerPhone ?? "-";
                worksheet.Cell(row, 5).Value = sale.CustomerEmail ?? "-";
                worksheet.Cell(row, 6).Value = sale.SubTotal;
                worksheet.Cell(row, 7).Value = sale.TaxAmount;
                worksheet.Cell(row, 8).Value = sale.DiscountAmount;
                worksheet.Cell(row, 9).Value = sale.TotalAmount;
                worksheet.Cell(row, 10).Value = GetPaymentStatusText(sale.PaymentStatus);

                // Currency formatting
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₺";
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00 ₺";
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00 ₺";

                row++;
            }

            // Totals
            worksheet.Cell(row, 8).Value = "TOPLAM:";
            worksheet.Cell(row, 8).Style.Font.Bold = true;
            worksheet.Cell(row, 9).Value = sales.Sum(s => s.TotalAmount);
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00 ₺";
            worksheet.Cell(row, 9).Style.Font.Bold = true;
            worksheet.Cell(row, 9).Style.Fill.BackgroundColor = XLColor.LightYellow;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return as byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportStocksAsync(int userId)
        {
            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.ProductName)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stoklar");

            // Header styling
            var headerRange = worksheet.Range("A1:K1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(1, 1).Value = "Ürün Kodu";
            worksheet.Cell(1, 2).Value = "Ürün Adı";
            worksheet.Cell(1, 3).Value = "Barkod";
            worksheet.Cell(1, 4).Value = "Kategori";
            worksheet.Cell(1, 5).Value = "Birim";
            worksheet.Cell(1, 6).Value = "Miktar";
            worksheet.Cell(1, 7).Value = "Alış Fiyatı";
            worksheet.Cell(1, 8).Value = "Satış Fiyatı";
            worksheet.Cell(1, 9).Value = "KDV %";
            worksheet.Cell(1, 10).Value = "Min. Stok";
            worksheet.Cell(1, 11).Value = "Toplam Değer";

            // Data
            int row = 2;
            foreach (var stock in stocks)
            {
                var totalValue = stock.Quantity * stock.PurchasePrice;
                var lowStock = stock.MinimumStock.HasValue && stock.Quantity <= stock.MinimumStock.Value;

                worksheet.Cell(row, 1).Value = stock.ProductCode;
                worksheet.Cell(row, 2).Value = stock.ProductName;
                worksheet.Cell(row, 3).Value = stock.Barcode ?? "-";
                worksheet.Cell(row, 4).Value = stock.Category ?? "-";
                worksheet.Cell(row, 5).Value = stock.Unit ?? "Adet";
                worksheet.Cell(row, 6).Value = stock.Quantity;
                worksheet.Cell(row, 7).Value = stock.PurchasePrice;
                worksheet.Cell(row, 8).Value = stock.SalePrice;
               // worksheet.Cell(row, 9).Value = stock.TaxRate;
                worksheet.Cell(row, 10).Value = stock.MinimumStock ?? 0;
                worksheet.Cell(row, 11).Value = totalValue;

                // Formatting
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00 ₺";
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00 ₺";
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "#0.00";
                worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00 ₺";

                // Low stock warning
                if (lowStock)
                {
                    worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightPink;
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
                }

                row++;
            }

            // Totals
            worksheet.Cell(row, 10).Value = "TOPLAM:";
            worksheet.Cell(row, 10).Style.Font.Bold = true;
            worksheet.Cell(row, 11).Value = stocks.Sum(s => s.Quantity * s.PurchasePrice);
            worksheet.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00 ₺";
            worksheet.Cell(row, 11).Style.Font.Bold = true;
            worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.LightYellow;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportPaymentsAsync(DateTime startDate, DateTime endDate, int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.Sale)
                .Where(p => p.UserId == userId && p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ödemeler");

            // Header styling
            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightCoral;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(1, 1).Value = "Tarih";
            worksheet.Cell(1, 2).Value = "Fatura No";
            worksheet.Cell(1, 3).Value = "Müşteri";
            worksheet.Cell(1, 4).Value = "Ödeme Tipi";
            worksheet.Cell(1, 5).Value = "Ödeme Yöntemi";
            worksheet.Cell(1, 6).Value = "Tutar";
            worksheet.Cell(1, 7).Value = "Açıklama";

            // Data
            int row = 2;
            decimal totalIncome = 0;
            decimal totalExpense = 0;

            foreach (var payment in payments)
            {
                worksheet.Cell(row, 1).Value = payment.PaymentDate.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 2).Value = payment.Sale?.SaleNumber ?? "-";
                worksheet.Cell(row, 3).Value = payment.CustomerName ?? "-";
                worksheet.Cell(row, 4).Value = GetPaymentTypeText(payment.PaymentType);
                worksheet.Cell(row, 5).Value = GetPaymentMethodText(payment.PaymentMethod);
                worksheet.Cell(row, 6).Value = payment.Amount;
                worksheet.Cell(row, 7).Value = payment.Notes ?? "-";

                // Currency formatting
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₺";

                // Color coding
                if (payment.PaymentType == Models.Entities.PaymentType.Income)
                {
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Green;
                    totalIncome += payment.Amount;
                }
                else
                {
                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
                    totalExpense += payment.Amount;
                }

                row++;
            }

            // Summary
            row++;
            worksheet.Cell(row, 5).Value = "Toplam Gelir:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = totalIncome;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₺";
            worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Green;
            worksheet.Cell(row, 6).Style.Font.Bold = true;

            row++;
            worksheet.Cell(row, 5).Value = "Toplam Gider:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = totalExpense;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₺";
            worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
            worksheet.Cell(row, 6).Style.Font.Bold = true;

            row++;
            worksheet.Cell(row, 5).Value = "Net:";
            worksheet.Cell(row, 5).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Value = totalIncome - totalExpense;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00 ₺";
            worksheet.Cell(row, 6).Style.Font.Bold = true;
            worksheet.Cell(row, 6).Style.Fill.BackgroundColor = XLColor.LightYellow;

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string GetPaymentStatusText(Models.Entities.PaymentStatus status)
        {
            return status switch
            {
                Models.Entities.PaymentStatus.Pending => "Beklemede",
                Models.Entities.PaymentStatus.PartialPaid => "Kısmi Ödendi",
                Models.Entities.PaymentStatus.Paid => "Ödendi",
                Models.Entities.PaymentStatus.Cancelled => "İptal",
                _ => "Bilinmiyor"
            };
        }

        private static string GetPaymentTypeText(Models.Entities.PaymentType type)
        {
            return type switch
            {
                Models.Entities.PaymentType.Income => "Gelir",
                Models.Entities.PaymentType.Expense => "Gider",
                _ => "Bilinmiyor"
            };
        }

        private static string GetPaymentMethodText(Models.Entities.PaymentMethod method)
        {
            return method switch
            {
                Models.Entities.PaymentMethod.Cash => "Nakit",
                Models.Entities.PaymentMethod.CreditCard => "Kredi Kartı",
                Models.Entities.PaymentMethod.BankTransfer => "Banka Havalesi",
                Models.Entities.PaymentMethod.Check => "Çek",
                Models.Entities.PaymentMethod.Other => "Diğer",
                _ => "Bilinmiyor"
            };
        }
    }
}