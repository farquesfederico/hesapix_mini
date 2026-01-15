namespace Hesapix.Services.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateSaleInvoicePdfAsync(int saleId, int userId);
}