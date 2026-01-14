namespace Hesapix.Services.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(int saleId, int userId);
        Task<byte[]> GenerateSalesReportPdfAsync(DateTime startDate, DateTime endDate, int userId);
        Task<byte[]> GenerateStockReportPdfAsync(int userId);
    }
}