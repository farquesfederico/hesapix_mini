using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SaleService> _logger;

        public SaleService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<SaleService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<SaleDto>>> GetSalesByUserIdAsync(
            int userId,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            var query = _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Stock)
                .Where(s => s.UserId == userId); // Güvenlik: Sadece kendi verilerine erişim

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.SaleDate <= endDate.Value);
            }

            var totalCount = await query.CountAsync();

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var saleDtos = _mapper.Map<List<SaleDto>>(sales);

            return ApiResponse<List<SaleDto>>.SuccessResult(saleDtos, $"Toplam {totalCount} satış bulundu");
        }

        public async Task<ApiResponse<SaleDto>> GetSaleByIdAsync(int saleId, int userId)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Stock)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == saleId && s.UserId == userId); // Güvenlik kontrolü

            if (sale == null)
            {
                return ApiResponse<SaleDto>.FailResult("Satış bulunamadı veya erişim yetkiniz yok");
            }

            var saleDto = _mapper.Map<SaleDto>(sale);
            return ApiResponse<SaleDto>.SuccessResult(saleDto);
        }

        public async Task<ApiResponse<SaleDto>> CreateSaleAsync(CreateSaleRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Stok kontrolü
                foreach (var item in request.Items)
                {
                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.Id == item.StokId && s.UserId == userId); // Güvenlik kontrolü

                    if (stock == null)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<SaleDto>.FailResult($"Stok bulunamadı: {item.StokId}");
                    }

                    if (stock.Quantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<SaleDto>.FailResult($"Yetersiz stok: {stock.ProductName} (Mevcut: {stock.Quantity}, İstenen: {item.Quantity})");
                    }
                }

                var sale = new Sale
                {
                    UserId = userId,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    CustomerEmail = request.CustomerEmail,
                    SaleDate = request.SaleDate ?? DateTime.UtcNow,
                    TotalAmount = 0,
                    PaidAmount = request.PaidAmount,
                    PaymentMethod = request.PaymentMethod,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;

                foreach (var item in request.Items)
                {
                    var stock = await _context.Stocks.FindAsync(item.StokId);

                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        StokId = item.StokId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice
                    };

                    _context.SaleItems.Add(saleItem);
                    totalAmount += saleItem.TotalPrice;

                    // Stok güncelleme
                    stock!.Quantity -= item.Quantity;
                }

                sale.TotalAmount = totalAmount;
                sale.RemainingAmount = totalAmount - request.PaidAmount;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var saleDto = await GetSaleByIdAsync(sale.Id, userId);
                return saleDto;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Satış oluşturulurken hata");
                return ApiResponse<SaleDto>.FailResult("Satış oluşturulamadı");
            }
        }

        public async Task<ApiResponse<SaleDto>> UpdateSaleAsync(int saleId, CreateSaleRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == saleId && s.UserId == userId); // Güvenlik kontrolü

                if (sale == null)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<SaleDto>.FailResult("Satış bulunamadı veya erişim yetkiniz yok");
                }

                // Eski stokları geri yükle
                foreach (var oldItem in sale.SaleItems)
                {
                    var stock = await _context.Stocks.FindAsync(oldItem.StokId);
                    if (stock != null)
                    {
                        stock.Quantity += (int)oldItem.Quantity;
                    }
                }

                // Eski satış itemlerini sil
                _context.SaleItems.RemoveRange(sale.SaleItems);

                // Yeni stok kontrolü
                foreach (var item in request.Items)
                {
                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.Id == item.StokId && s.UserId == userId); // Güvenlik kontrolü

                    if (stock == null)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<SaleDto>.FailResult($"Stok bulunamadı: {item.StokId}");
                    }

                    if (stock.Quantity < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return ApiResponse<SaleDto>.FailResult($"Yetersiz stok: {stock.ProductName}");
                    }
                }

                // Satış bilgilerini güncelle
                sale.CustomerName = request.CustomerName;
                sale.CustomerPhone = request.CustomerPhone;
                sale.CustomerEmail = request.CustomerEmail;
                sale.SaleDate = request.SaleDate ?? sale.SaleDate;
                sale.PaidAmount = request.PaidAmount;
                sale.PaymentMethod = request.PaymentMethod;
                sale.Notes = request.Notes;
                sale.UpdatedAt = DateTime.UtcNow;

                decimal totalAmount = 0;

                // Yeni satış itemlerini ekle
                foreach (var item in request.Items)
                {
                    var stock = await _context.Stocks.FindAsync(item.StokId);

                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        StokId = item.StokId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice
                    };

                    _context.SaleItems.Add(saleItem);
                    totalAmount += saleItem.TotalPrice;

                    stock!.Quantity -= item.Quantity;
                }

                sale.TotalAmount = totalAmount;
                sale.RemainingAmount = totalAmount - request.PaidAmount;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var saleDto = await GetSaleByIdAsync(sale.Id, userId);
                return saleDto;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Satış güncellenirken hata: SaleId={SaleId}", saleId);
                return ApiResponse<SaleDto>.FailResult("Satış güncellenemedi");
            }
        }

        public async Task<ApiResponse<bool>> DeleteSaleAsync(int saleId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == saleId && s.UserId == userId); // Güvenlik kontrolü

                if (sale == null)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<bool>.FailResult("Satış bulunamadı veya erişim yetkiniz yok");
                }

                // Stokları geri yükle
                foreach (var item in sale.SaleItems)
                {
                    var stock = await _context.Stocks.FindAsync(item.StokId);
                    if (stock != null)
                    {
                        stock.Quantity += item.Quantity;
                    }
                }

                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponse<bool>.SuccessResult(true, "Satış silindi");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Satış silinirken hata: SaleId={SaleId}", saleId);
                return ApiResponse<bool>.FailResult("Satış silinemedi");
            }
        }
    }
}