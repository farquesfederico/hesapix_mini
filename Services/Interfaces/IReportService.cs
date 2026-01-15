using Hesapix.Models.DTOs.Report;

namespace Hesapix.Services.Interfaces;

public interface IReportService
{
    Task<DashboardReportDto> GetDashboardReportAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
}