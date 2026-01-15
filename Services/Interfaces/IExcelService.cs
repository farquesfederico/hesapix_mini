namespace Hesapix.Services.Interfaces;

public interface IExcelService
{
    Task<byte[]> ExportSalesToExcelAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportPaymentsToExcelAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportStocksToExcelAsync(int userId);
}