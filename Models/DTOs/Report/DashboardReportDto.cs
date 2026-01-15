namespace Hesapix.Models.DTOs.Report;

public class DashboardReportDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetProfit { get; set; }
    public int PendingSalesCount { get; set; }
    public int TotalProductCount { get; set; }
    public int LowStockCount { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<MonthlySalesSummary> MonthlySales { get; set; } = new();
}

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlySalesSummary
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalAmount { get; set; }
    public int SalesCount { get; set; }
}