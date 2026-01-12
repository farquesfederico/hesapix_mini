using Hesapix.Models.DTOs.Stock;

namespace Hesapix.Services.Interfaces
{
    public interface IStockService
    {
        Task<List<StockDto>> GetAllStocks(int userId);
        Task<StockDto> GetStockById(int id, int userId);
        Task<StockDto> GetStockByCode(string productCode, int userId);
        Task<StockDto> CreateStock(StockDto dto, int userId);
        Task<StockDto> UpdateStock(int id, StockDto dto, int userId);
        Task<bool> DeleteStock(int id, int userId);
        Task<bool> UpdateStockQuantity(int stockId, decimal quantity, int userId);
        Task<List<StockDto>> GetLowStocks(int userId);
        Task<List<StockDto>> SearchStocks(string searchTerm, int userId);
    }
}