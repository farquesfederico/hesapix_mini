using Hesapix.Models.DTOs.Sale;

namespace Hesapix.Services.Interfaces
{
    public interface ISaleService
    {
        Task<SaleDto> CreateSale(CreateSaleRequest request, int userId);
        Task<List<SaleDto>> GetSales(int userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<SaleDto> GetSaleById(int id, int userId);
        Task<SaleDto> GetSaleByNumber(string saleNumber, int userId);
        Task<bool> CancelSale(int id, int userId);
        Task<List<SaleDto>> GetPendingPaymentSales(int userId);
        Task<bool> UpdateSalePaymentStatus(int saleId, int userId);
    }
}