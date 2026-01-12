using Hesapix.Models.DTOs.Report;

namespace Hesapix.Services.Interfaces
{
    public interface IReportService
    {
        Task<DashboardReportDto> GetDashboardReport(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<byte[]> GenerateSalesReportPdf(int userId, DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateStockReportExcel(int userId);
    }
}