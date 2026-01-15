using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Stock;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations;

public class StokService : IStokService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public StokService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<StockDto>> GetStocksAsync(int userId, int pageNumber = 1, int pageSize = 10,
        string? searchTerm = null, bool? lowStockOnly = null)
    {
        var query = _context.Stocks.Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s =>
                s.ProductCode.Contains(searchTerm) ||
                s.ProductName.Contains(searchTerm) ||
                (s.Barcode != null && s.Barcode.Contains(searchTerm)));
        }

        if (lowStockOnly == true)
        {
            query = query.Where(s => s.Quantity <= s.MinimumStock);
        }

        var totalCount = await query.CountAsync();

        var stocks = await query
            .OrderBy(s => s.ProductName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StockDto>
        {
            Items = _mapper.Map<List<StockDto>>(stocks),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<StockDto?> GetStockByIdAsync(int id, int userId)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        return stock != null ? _mapper.Map<StockDto>(stock) : null;
    }

    public async Task<(bool Success, string Message, StockDto? Data)> CreateStockAsync(CreateStockRequest request, int userId)
    {
        // Ürün kodu kontrolü
        if (await _context.Stocks.AnyAsync(s => s.UserId == userId && s.ProductCode == request.ProductCode))
        {
            return (false, "Bu ürün kodu zaten mevcut", null);
        }

        var stock = _mapper.Map<Stok>(request);
        stock.UserId = userId;
        stock.CreatedAt = DateTime.UtcNow;

        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync();

        var createdStock = _mapper.Map<StockDto>(stock);
        return (true, "Stok başarıyla oluşturuldu", createdStock);
    }

    public async Task<(bool Success, string Message, StockDto? Data)> UpdateStockAsync(int id, UpdateStockRequest request, int userId)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (stock == null)
        {
            return (false, "Stok bulunamadı", null);
        }

        // Farklı bir ürün kodu varsa kontrol et
        if (stock.ProductCode != request.ProductCode)
        {
            if (await _context.Stocks.AnyAsync(s => s.UserId == userId && s.ProductCode == request.ProductCode && s.Id != id))
            {
                return (false, "Bu ürün kodu başka bir üründe kullanılıyor", null);
            }
        }

        stock.ProductCode = request.ProductCode;
        stock.ProductName = request.ProductName;
        stock.Category = request.Category;
        stock.Unit = request.Unit;
        stock.Quantity = request.Quantity;
        stock.MinimumStock = request.MinimumStock;
        stock.PurchasePrice = request.PurchasePrice;
        stock.SalePrice = request.SalePrice;
        stock.TaxRate = request.TaxRate;
        stock.Barcode = request.Barcode;
        stock.Description = request.Description;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updatedStock = _mapper.Map<StockDto>(stock);
        return (true, "Stok başarıyla güncellendi", updatedStock);
    }

    public async Task<(bool Success, string Message)> DeleteStockAsync(int id, int userId)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (stock == null)
        {
            return (false, "Stok bulunamadı");
        }

        // Satışlarda kullanılıp kullanılmadığını kontrol et
        var hasUsage = await _context.SaleItems.AnyAsync(si => si.StockId == id);
        if (hasUsage)
        {
            // Soft delete
            stock.IsDeleted = true;
            stock.DeletedAt = DateTime.UtcNow;
            stock.IsActive = false;
        }
        else
        {
            // Hard delete
            _context.Stocks.Remove(stock);
        }

        await _context.SaveChangesAsync();
        return (true, "Stok başarıyla silindi");
    }

    public async Task<bool> UpdateStockQuantityAsync(int stockId, int quantity, int userId)
    {
        var stock = await _context.Stocks
            .FirstOrDefaultAsync(s => s.Id == stockId && s.UserId == userId);

        if (stock == null)
        {
            return false;
        }

        stock.Quantity = quantity;
        stock.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}