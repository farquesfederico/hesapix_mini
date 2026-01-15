using Hesapix.Models.DTOs.Sale;
using Hesapix.Services.Implementations;
using Hesapix.Models.Common;


namespace Hesapix.Services.Interfaces
{
    public interface ISaleService
    {
        Task<SaleDto> CreateSale(CreateSaleRequest request, int userId);

        // Pagination destekli
        Task<PagedResult<SaleDto>> GetSales(int userId, int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null);

        Task<SaleDto> GetSaleById(int id, int userId);
        Task<SaleDto> GetSaleByNumber(string saleNumber, int userId);
        Task<bool> CancelSale(int id, int userId);

        // Pagination destekli
        Task<PagedResult<SaleDto>> GetPendingPaymentSales(int userId, int page = 1, int pageSize = 20);

        Task<bool> UpdateSalePaymentStatus(int saleId, int userId);
    }
}
