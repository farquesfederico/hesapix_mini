using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;

        public StockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockDto>> GetAllStocks(int userId)
        {
            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderBy(s => s.ProductName)
                .ToListAsync();

            return stocks.Select(MapToDto).ToList();
        }

        public async Task<StockDto> GetStockById(int id, int userId)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsActive);

            if (stock == null)
            {
                throw new Exception("Stok bulunamadı");
            }

            return MapToDto(stock);
        }

        public async Task<StockDto> GetStockByCode(string productCode, int userId)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductCode == productCode && s.UserId == userId && s.IsActive);

            if (stock == null)
            {
                throw new Exception("Stok bulunamadı");
            }

            return MapToDto(stock);
        }

        public async Task<StockDto> CreateStock(StockDto dto, int userId)
        {
            // Aynı ürün kodunun olup olmadığını kontrol et
            var exists = await _context.Stocks
                .AnyAsync(s => s.UserId == userId && s.ProductCode == dto.ProductCode && s.IsActive);

            if (exists)
            {
                throw new Exception("Bu ürün kodu zaten mevcut");
            }

            var stock = new Stock
            {
                UserId = userId,
                ProductCode = dto.ProductCode,
                ProductName = dto.ProductName,
                Description = dto.Description,
                Category = dto.Category,
                Unit = dto.Unit,
                Quantity = dto.Quantity,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                MinimumStock = dto.MinimumStock,
                Barcode = dto.Barcode,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();

            return MapToDto(stock);
        }

        public async Task<StockDto> UpdateStock(int id, StockDto dto, int userId)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsActive);

            if (stock == null)
            {
                throw new Exception("Stok bulunamadı");
            }

            // Ürün kodu değiştiriliyorsa, başka bir üründe kullanılmadığını kontrol et
            if (stock.ProductCode != dto.ProductCode)
            {
                var codeExists = await _context.Stocks
                    .AnyAsync(s => s.UserId == userId && s.ProductCode == dto.ProductCode && s.Id != id && s.IsActive);

                if (codeExists)
                {
                    throw new Exception("Bu ürün kodu başka bir ürün tarafından kullanılıyor");
                }
            }

            stock.ProductCode = dto.ProductCode;
            stock.ProductName = dto.ProductName;
            stock.Description = dto.Description;
            stock.Category = dto.Category;
            stock.Unit = dto.Unit;
            stock.Quantity = dto.Quantity;
            stock.PurchasePrice = dto.PurchasePrice;
            stock.SalePrice = dto.SalePrice;
            stock.MinimumStock = dto.MinimumStock;
            stock.Barcode = dto.Barcode;
            stock.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToDto(stock);
        }

        public async Task<bool> DeleteStock(int id, int userId)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsActive);

            if (stock == null)
            {
                return false;
            }

            // Soft delete
            stock.IsActive = false;
            stock.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateStockQuantity(int stockId, decimal quantity, int userId)
        {
            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.Id == stockId && s.UserId == userId && s.IsActive);

            if (stock == null)
            {
                return false;
            }

            stock.Quantity += quantity;
            stock.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<StockDto>> GetLowStocks(int userId)
        {
            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId &&
                           s.IsActive &&
                           s.MinimumStock.HasValue &&
                           s.Quantity <= s.MinimumStock.Value)
                .OrderBy(s => s.Quantity)
                .ToListAsync();

            return stocks.Select(MapToDto).ToList();
        }

        public async Task<List<StockDto>> SearchStocks(string searchTerm, int userId)
        {
            var stocks = await _context.Stocks
                .Where(s => s.UserId == userId &&
                           s.IsActive &&
                           (s.ProductName.Contains(searchTerm) ||
                            s.ProductCode.Contains(searchTerm) ||
                            (s.Barcode != null && s.Barcode.Contains(searchTerm))))
                .OrderBy(s => s.ProductName)
                .ToListAsync();

            return stocks.Select(MapToDto).ToList();
        }

        #region Private Methods

        private StockDto MapToDto(Stock stock)
        {
            return new StockDto
            {
                Id = stock.Id,
                ProductCode = stock.ProductCode,
                ProductName = stock.ProductName,
                Description = stock.Description,
                Category = stock.Category,
                Unit = stock.Unit,
                Quantity = stock.Quantity,
                PurchasePrice = stock.PurchasePrice,
                SalePrice = stock.SalePrice,
                MinimumStock = stock.MinimumStock,
                Barcode = stock.Barcode
            };
        }

        #endregion
    }
}