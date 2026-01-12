using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Models.DTOs.Report;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using System.Globalization;

namespace Hesapix.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardReportDto> GetDashboardReport(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Tarih aralığı belirtilmemişse son 30 gün
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today;

            // Satışlar
            var sales = await _context.Sales
                .Where(s => s.UserId == userId &&
                           s.SaleDate >= startDate &&
                           s.SaleDate <= endDate &&
                           s.PaymentStatus != PaymentStatus.Cancelled)
                .ToListAsync();

            // Ödemeler
            var payments = await _context.Payments
                .Where(p => p.UserId == userId &&
                           p.PaymentDate >= startDate &&
                           p.PaymentDate <= endDate)
                .ToListAsync();

            // Bekleyen ödemeler
            var pendingSales = await _context.Sales
                .Where(s => s.UserId == userId &&
                           (s.PaymentStatus == PaymentStatus.Pending ||
                            s.PaymentStatus == PaymentStatus.PartialPaid))
                .ToListAsync();

            // Düşük stoklar
            var lowStocks = await _context.Stocks
                .Where(s => s.UserId == userId &&
                           s.IsActive &&
                           s.MinimumStock.HasValue &&
                           s.Quantity <= s.MinimumStock.Value)
                .CountAsync();

            // En çok satan ürünler
            var topProducts = await _context.SaleItems
                .Include(si => si.Sale)
                .Where(si => si.Sale.UserId == userId &&
                            si.Sale.SaleDate >= startDate &&
                            si.Sale.SaleDate <= endDate &&
                            si.Sale.PaymentStatus != PaymentStatus.Cancelled)
                .GroupBy(si => si.ProductName)
                .Select(g => new TopSellingProductDto
                {
                    ProductName = g.Key,
                    TotalQuantity = g.Sum(si => si.Quantity),
                    TotalAmount = g.Sum(si => si.TotalPrice)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(5)
                .ToListAsync();

            // Aylık satışlar (son 6 ay)
            var sixMonthsAgo = DateTime.Today.AddMonths(-6);
            var monthlySalesData = await _context.Sales
                .Where(s => s.UserId == userId &&
                           s.SaleDate >= sixMonthsAgo &&
                           s.PaymentStatus != PaymentStatus.Cancelled)
                .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                .Select(g => new MonthlySalesDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(g.Key.Month),
                    TotalAmount = g.Sum(s => s.TotalAmount),
                    SalesCount = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            return new DashboardReportDto
            {
                TotalSales = sales.Sum(s => s.TotalAmount),
                TotalIncome = payments.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount),
                TotalExpense = payments.Where(p => p.PaymentType == PaymentType.Expense).Sum(p => p.Amount),
                PendingPayments = pendingSales.Sum(s => s.TotalAmount) -
                                 pendingSales.Sum(s => s.Payments?.Where(p => p.PaymentType == PaymentType.Income).Sum(p => p.Amount) ?? 0),
                TotalSalesCount = sales.Count,
                TotalPaymentsCount = payments.Count,
                LowStockCount = lowStocks,
                TopSellingProducts = topProducts,
                MonthlySales = monthlySalesData
            };
        }

        public async Task<byte[]> GenerateSalesReportPdf(int userId, DateTime startDate, DateTime endDate)
        {
            // PDF oluşturma için iTextSharp veya QuestPDF kütüphanesi kullanılabilir
            // Bu metod şu an için placeholder olarak bırakılmıştır
            throw new NotImplementedException("PDF rapor özelliği henüz geliştirilmedi");
        }

        public async Task<byte[]> GenerateStockReportExcel(int userId)
        {
            // Excel oluşturma için EPPlus veya ClosedXML kütüphanesi kullanılabilir
            // Bu metod şu an için placeholder olarak bırakılmıştır
            throw new NotImplementedException("Excel rapor özelliği henüz geliştirilmedi");
        }
    }
}