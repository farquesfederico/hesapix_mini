using AutoMapper;
using Hesapix.Controllers;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations
{
    public class StokService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StokService> _logger;

        public StokService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<StokService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<StockDto>>> GetStocksByUserIdAsync(int userId, string? search, int page, int pageSize)
        {
            try
            {
                var query = _context.Stocks
                    .Where(s => s.UserId == userId);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(s =>
                        s.ProductName.ToLower().Contains(search) ||
                        (s.ProductCode != null && s.ProductCode.ToLower().Contains(search)) ||
                        (s.Category != null && s.Category.ToLower().Contains(search)));
                }

                var totalCount = await query.CountAsync();

                var stocks = await query
                    .OrderBy(s => s.ProductName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                var stockDtos = _mapper.Map<List<StockDto>>(stocks);

                return ApiResponse<List<StockDto>>.SuccessResult(stockDtos, $"Toplam {totalCount} stok bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stoklar listeleme hatası");
                return ApiResponse<List<StockDto>>.FailResult("Stoklar listelenemedi");
            }
        }

        public async Task<ApiResponse<StockDto>> GetStockByIdAsync(int stockId, int userId)
        {
            try
            {
                var stock = await _context.Stocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == stockId && s.UserId == userId);

                if (stock == null)
                {
                    return ApiResponse<StockDto>.FailResult("Stok bulunamadı veya erişim yetkiniz yok");
                }

                var stockDto = _mapper.Map<StockDto>(stock);
                return ApiResponse<StockDto>.SuccessResult(stockDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok bilgisi alma hatası");
                return ApiResponse<StockDto>.FailResult("Stok bilgisi alınamadı");
            }
        }

        public async Task<ApiResponse<StockDto>> CreateStockAsync(CreateStockRequest request, int userId)
        {
            try
            {
                var stock = new Stok
                {
                    UserId = userId,
                    ProductName = request.ProductName,
                    ProductCode = request.ProductCode,
                    Category = request.Category,
                    Quantity = request.Quantity,
                    UnitPrice = request.UnitPrice,
                    CostPrice = request.CostPrice,
                    Unit = request.Unit,
                    MinStockLevel = request.MinStockLevel,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Stocks.Add(stock);
                await _context.SaveChangesAsync();

                var stockDto = _mapper.Map<StockDto>(stock);
                return ApiResponse<StockDto>.SuccessResult(stockDto, "Stok oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok oluşturma hatası");
                return ApiResponse<StockDto>.FailResult("Stok oluşturulamadı");
            }
        }

        public async Task<ApiResponse<StockDto>> UpdateStockAsync(int stockId, CreateStockRequest request, int userId)
        {
            try
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.Id == stockId && s.UserId == userId);

                if (stock == null)
                {
                    return ApiResponse<StockDto>.FailResult("Stok bulunamadı veya erişim yetkiniz yok");
                }

                stock.ProductName = request.ProductName;
                stock.ProductCode = request.ProductCode;
                stock.Category = request.Category;
                stock.Quantity = request.Quantity;
                stock.UnitPrice = request.UnitPrice;
                stock.CostPrice = request.CostPrice;
                stock.Unit = request.Unit;
                stock.MinStockLevel = request.MinStockLevel;
                stock.Description = request.Description;
                stock.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var stockDto = _mapper.Map<StockDto>(stock);
                return ApiResponse<StockDto>.SuccessResult(stockDto, "Stok güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok güncelleme hatası");
                return ApiResponse<StockDto>.FailResult("Stok güncellenemedi");
            }
        }

        public async Task<ApiResponse<bool>> DeleteStockAsync(int stockId, int userId)
        {
            try
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.Id == stockId && s.UserId == userId);

                if (stock == null)
                {
                    return ApiResponse<bool>.FailResult("Stok bulunamadı veya erişim yetkiniz yok");
                }

                var hasRelatedSales = await _context.SaleItems
                    .AnyAsync(si => si.StokId == stockId);

                if (hasRelatedSales)
                {
                    return ApiResponse<bool>.FailResult("Bu stok satış işlemlerinde kullanılmış, silinemez");
                }

                _context.Stocks.Remove(stock);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Stok silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok silme hatası");
                return ApiResponse<bool>.FailResult("Stok silinemedi");
            }
        }

        public async Task<ApiResponse<List<StockDto>>> GetLowStockItemsAsync(int userId, int threshold)
        {
            try
            {
                var stocks = await _context.Stocks
                    .Where(s => s.UserId == userId &&
                               s.MinStockLevel.HasValue &&
                               s.Quantity <= s.MinStockLevel.Value)
                    .AsNoTracking()
                    .ToListAsync();

                var stockDtos = _mapper.Map<List<StockDto>>(stocks);
                return ApiResponse<List<StockDto>>.SuccessResult(stockDtos, $"{stocks.Count} düşük stoklu ürün bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Düşük stok listesi alma hatası");
                return ApiResponse<List<StockDto>>.FailResult("Düşük stok listesi alınamadı");
            }
        }
    }
}