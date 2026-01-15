using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;

namespace Hesapix.Services.Interfaces;

public interface IStokService
{
    Task<PagedResult<StockDto>> GetStocksAsync(int userId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null, bool? lowStockOnly = null);
    Task<StockDto?> GetStockByIdAsync(int id, int userId);
    Task<(bool Success, string Message, StockDto? Data)> CreateStockAsync(CreateStockRequest request, int userId);
    Task<(bool Success, string Message, StockDto? Data)> UpdateStockAsync(int id, UpdateStockRequest request, int userId);
    Task<(bool Success, string Message)> DeleteStockAsync(int id, int userId);
    Task<bool> UpdateStockQuantityAsync(int stockId, int quantity, int userId);
}