using Hesapix.Data;
using Hesapix.Models.DTOs.Report;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardReportDto> GetDashboardReportAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddMonths(-1);
        endDate ??= DateTime.UtcNow;

        // Satış toplamları
        var salesQuery = _context.Sales
            .Where(s => s.UserId == userId
                && s.SaleDate >= startDate
                && s.SaleDate <= endDate
                && s.PaymentStatus != PaymentStatus.Cancelled);

        var totalSales = await salesQuery.SumAsync(s => (decimal?)s.TotalAmount) ?? 0;
        var pendingSalesCount = await salesQuery.CountAsync(s => s.PaymentStatus == PaymentStatus.Pending);

        // Gelir/Gider
        var paymentsQuery = _context.Payments
            .Where(p => p.UserId == userId
                && p.PaymentDate >= startDate
                && p.PaymentDate <= endDate);

        var totalIncome = await paymentsQuery
            .Where(p => p.PaymentType == PaymentType.Income)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var totalExpense = await paymentsQuery
            .Where(p => p.PaymentType == PaymentType.Expense)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        // Stok bilgileri
        var stocksQuery = _context.Stocks.Where(s => s.UserId == userId && s.IsActive);
        var totalProductCount = await stocksQuery.CountAsync();
        var lowStockCount = await stocksQuery.CountAsync(s => s.Quantity <= s.MinimumStock);

        // En çok satan ürünler
        var topProducts = await _context.SaleItems
            .Where(si => si.Sale.UserId == userId
                && si.Sale.SaleDate >= startDate
                && si.Sale.SaleDate <= endDate
                && si.Sale.PaymentStatus != PaymentStatus.Cancelled)
            .GroupBy(si => new { si.ProductName })
            .Select(g => new TopProductDto
            {
                ProductName = g.Key.ProductName,
                TotalQuantity = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.TotalPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToListAsync();

        // Aylık satış özeti (son 6 ay)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlySales = await _context.Sales
            .Where(s => s.UserId == userId
                && s.SaleDate >= sixMonthsAgo
                && s.PaymentStatus != PaymentStatus.Cancelled)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new MonthlySalesSummary
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(s => s.TotalAmount),
                SalesCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync();

        return new DashboardReportDto
        {
            TotalSales = totalSales,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetProfit = totalIncome - totalExpense,
            PendingSalesCount = pendingSalesCount,
            TotalProductCount = totalProductCount,
            LowStockCount = lowStockCount,
            TopProducts = topProducts,
            MonthlySales = monthlySales
        };
    }
}