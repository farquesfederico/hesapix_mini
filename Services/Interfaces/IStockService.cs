using Hesapix.Controllers;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;

namespace Hesapix.Services.Interfaces
{
    public interface IStockService
    {
        Task<ApiResponse<List<StockDto>>> GetStocksByUserIdAsync(int userId, string? search, int page, int pageSize);
        Task<ApiResponse<StockDto>> GetStockByIdAsync(int stockId, int userId);
        Task<ApiResponse<StockDto>> CreateStockAsync(CreateStockRequest request, int userId);
        Task<ApiResponse<StockDto>> UpdateStockAsync(int stockId, CreateStockRequest request, int userId);
        Task<ApiResponse<bool>> DeleteStockAsync(int stockId, int userId);
        Task<ApiResponse<List<StockDto>>> GetLowStockItemsAsync(int userId, int threshold);
    }
}