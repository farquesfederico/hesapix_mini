using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Sale;

namespace Hesapix.Services.Interfaces;

public interface ISaleService
{
    Task<PagedResult<SaleDto>> GetSalesAsync(int userId, int pageNumber = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null);
    Task<SaleDto?> GetSaleByIdAsync(int id, int userId);
    Task<SaleDto?> GetSaleByNumberAsync(string saleNumber, int userId);
    Task<(bool Success, string Message, SaleDto? Data)> CreateSaleAsync(CreateSaleRequest request, int userId);
    Task<(bool Success, string Message, SaleDto? Data)> UpdateSaleAsync(int id, CreateSaleRequest request, int userId);
    Task<(bool Success, string Message)> DeleteSaleAsync(int id, int userId);
    Task<(bool Success, string Message)> CancelSaleAsync(int id, int userId);
    Task<(bool Success, string Message)> UpdateSalePaymentStatusAsync(int id, int userId);
    Task<PagedResult<SaleDto>> GetPendingPaymentSalesAsync(int userId, int pageNumber = 1, int pageSize = 10);
}