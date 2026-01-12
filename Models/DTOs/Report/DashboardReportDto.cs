namespace Hesapix.Models.DTOs.Report
{
    public class DashboardReportDto
    {
        public decimal TotalSales { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal PendingPayments { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalPaymentsCount { get; set; }
        public int LowStockCount { get; set; }
        public List<TopSellingProductDto> TopSellingProducts { get; set; }
        public List<MonthlySalesDto> MonthlySales { get; set; }
    }

    public class TopSellingProductDto
    {
        public string ProductName { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MonthlySalesDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal TotalAmount { get; set; }
        public int SalesCount { get; set; }
    }
}