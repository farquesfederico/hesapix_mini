using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations;

public class SaleService : ISaleService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public SaleService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SaleDto>> GetSalesAsync(int userId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Sales
            .Include(s => s.SaleItems)
            .Where(s => s.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(s => s.SaleDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SaleDate <= endDate.Value);

        var totalCount = await query.CountAsync();

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SaleDto>
        {
            Items = _mapper.Map<List<SaleDto>>(sales),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<SaleDto?> GetSaleByIdAsync(int id, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        return sale != null ? _mapper.Map<SaleDto>(sale) : null;
    }

    public async Task<SaleDto?> GetSaleByNumberAsync(string saleNumber, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber && s.UserId == userId);

        return sale != null ? _mapper.Map<SaleDto>(sale) : null;
    }

    public async Task<(bool Success, string Message, SaleDto? Data)> CreateSaleAsync(CreateSaleRequest request, int userId)
    {
        if (!request.Items.Any())
        {
            return (false, "Satış kalemleri boş olamaz", null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Stok kontrolü
            foreach (var item in request.Items)
            {
                var stock = await _context.Stocks.FindAsync(item.StockId);
                if (stock == null || stock.UserId != userId)
                {
                    return (false, $"Stok bulunamadı: {item.StockId}", null);
                }

                if (stock.Quantity < item.Quantity)
                {
                    return (false, $"{stock.ProductName} için yetersiz stok", null);
                }
            }

            // Satış numarası oluştur
            var saleNumber = await GenerateSaleNumberAsync(userId);

            // Hesaplamalar
            decimal subTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            decimal taxAmount = subTotal * (request.TaxRate / 100);
            decimal totalAmount = subTotal + taxAmount - request.DiscountAmount;

            var sale = new Sale
            {
                UserId = userId,
                SaleNumber = saleNumber,
                SaleDate = request.SaleDate,
                CustomerName = request.CustomerName,
                CustomerTaxNumber = request.CustomerTaxNumber,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                SubTotal = subTotal,
                TaxRate = request.TaxRate,
                TaxAmount = taxAmount,
                DiscountAmount = request.DiscountAmount,
                TotalAmount = totalAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Sale items oluştur ve stok düş
            foreach (var itemRequest in request.Items)
            {
                var stock = await _context.Stocks.FindAsync(itemRequest.StockId);

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    StockId = itemRequest.StockId,
                    ProductCode = stock!.ProductCode,
                    ProductName = stock.ProductName,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    TotalPrice = itemRequest.Quantity * itemRequest.UnitPrice
                };

                _context.SaleItems.Add(saleItem);

                // Stok güncelle
                stock.Quantity -= itemRequest.Quantity;
                stock.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var createdSale = await GetSaleByIdAsync(sale.Id, userId);
            return (true, "Satış başarıyla oluşturuldu", createdSale);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Satış oluşturulurken hata: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, SaleDto? Data)> UpdateSaleAsync(int id, CreateSaleRequest request, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (sale == null)
        {
            return (false, "Satış bulunamadı", null);
        }

        if (sale.PaymentStatus == PaymentStatus.Paid)
        {
            return (false, "Ödeme yapılmış satışlar güncellenemez", null);
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Eski stokları geri ekle
            foreach (var oldItem in sale.SaleItems)
            {
                var stock = await _context.Stocks.FindAsync(oldItem.StockId);
                if (stock != null)
                {
                    stock.Quantity += oldItem.Quantity;
                }
            }

            // Eski itemları sil
            _context.SaleItems.RemoveRange(sale.SaleItems);
            await _context.SaveChangesAsync();

            // Yeni stok kontrolü
            foreach (var item in request.Items)
            {
                var stock = await _context.Stocks.FindAsync(item.StockId);
                if (stock == null || stock.UserId != userId)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Stok bulunamadı: {item.StockId}", null);
                }

                if (stock.Quantity < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    return (false, $"{stock.ProductName} için yetersiz stok", null);
                }
            }

            // Hesaplamalar
            decimal subTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            decimal taxAmount = subTotal * (request.TaxRate / 100);
            decimal totalAmount = subTotal + taxAmount - request.DiscountAmount;

            // Sale güncelle
            sale.SaleDate = request.SaleDate;
            sale.CustomerName = request.CustomerName;
            sale.CustomerTaxNumber = request.CustomerTaxNumber;
            sale.CustomerPhone = request.CustomerPhone;
            sale.CustomerAddress = request.CustomerAddress;
            sale.SubTotal = subTotal;
            sale.TaxRate = request.TaxRate;
            sale.TaxAmount = taxAmount;
            sale.DiscountAmount = request.DiscountAmount;
            sale.TotalAmount = totalAmount;
            sale.PaymentMethod = request.PaymentMethod;
            sale.Notes = request.Notes;
            sale.UpdatedAt = DateTime.UtcNow;

            // Yeni itemlar ekle
            foreach (var itemRequest in request.Items)
            {
                var stock = await _context.Stocks.FindAsync(itemRequest.StockId);

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    StockId = itemRequest.StockId,
                    ProductCode = stock!.ProductCode,
                    ProductName = stock.ProductName,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    TotalPrice = itemRequest.Quantity * itemRequest.UnitPrice
                };

                _context.SaleItems.Add(saleItem);
                stock.Quantity -= itemRequest.Quantity;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updatedSale = await GetSaleByIdAsync(sale.Id, userId);
            return (true, "Satış başarıyla güncellendi", updatedSale);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Satış güncellenirken hata: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteSaleAsync(int id, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (sale == null)
        {
            return (false, "Satış bulunamadı");
        }

        // Soft delete
        sale.IsDeleted = true;
        sale.DeletedAt = DateTime.UtcNow;

        // Stokları geri ekle
        foreach (var item in sale.SaleItems)
        {
            var stock = await _context.Stocks.FindAsync(item.StockId);
            if (stock != null)
            {
                stock.Quantity += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
        return (true, "Satış başarıyla silindi");
    }

    public async Task<(bool Success, string Message)> CancelSaleAsync(int id, int userId)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (sale == null)
        {
            return (false, "Satış bulunamadı");
        }

        sale.PaymentStatus = PaymentStatus.Cancelled;
        sale.UpdatedAt = DateTime.UtcNow;

        // Stokları geri ekle
        foreach (var item in sale.SaleItems)
        {
            var stock = await _context.Stocks.FindAsync(item.StockId);
            if (stock != null)
            {
                stock.Quantity += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
        return (true, "Satış iptal edildi");
    }

    public async Task<(bool Success, string Message)> UpdateSalePaymentStatusAsync(int id, int userId)
    {
        var sale = await _context.Sales.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (sale == null)
        {
            return (false, "Satış bulunamadı");
        }

        sale.PaymentStatus = PaymentStatus.Paid;
        sale.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Ödeme durumu güncellendi");
    }

    public async Task<PagedResult<SaleDto>> GetPendingPaymentSalesAsync(int userId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Sales
            .Include(s => s.SaleItems)
            .Where(s => s.UserId == userId && s.PaymentStatus == PaymentStatus.Pending);

        var totalCount = await query.CountAsync();

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SaleDto>
        {
            Items = _mapper.Map<List<SaleDto>>(sales),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<string> GenerateSaleNumberAsync(int userId)
    {
        var today = DateTime.UtcNow;
        var prefix = $"SAL-{today:yyyyMMdd}";

        var lastSale = await _context.Sales
            .Where(s => s.UserId == userId && s.SaleNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SaleNumber)
            .FirstOrDefaultAsync();

        if (lastSale == null)
        {
            return $"{prefix}-0001";
        }

        var lastNumber = int.Parse(lastSale.SaleNumber.Split('-').Last());
        return $"{prefix}-{(lastNumber + 1):D4}";
    }
}