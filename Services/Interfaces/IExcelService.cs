namespace Hesapix.Services.Interfaces
{
    public interface IExcelService
    {
        Task<byte[]> ExportSalesAsync(DateTime startDate, DateTime endDate, int userId);
        Task<byte[]> ExportStoksAsync(int userId);
        Task<byte[]> ExportPaymentsAsync(DateTime startDate, DateTime endDate, int userId);
    }
}