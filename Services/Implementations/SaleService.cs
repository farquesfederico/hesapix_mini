using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Models.DTOs.Sale;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _context;

        public SaleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SaleDto> CreateSale(CreateSaleRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Satış numarası oluştur
                var saleNumber = await GenerateSaleNumber(userId);

                decimal subTotal = 0;
                decimal taxAmount = 0;

                var saleItems = new List<SaleItem>();

                // Her ürün için kontrol ve hesaplama
                foreach (var item in request.Items)
                {
                    var stock = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.Id == item.StockId && s.UserId == userId && s.IsActive);

                    if (stock == null)
                    {
                        throw new Exception($"Stok bulunamadı: {item.StockId}");
                    }

                    if (stock.Quantity < item.Quantity)
                    {
                        throw new Exception($"{stock.ProductName} için yetersiz stok. Mevcut: {stock.Quantity}");
                    }

                    // Fiyat hesaplamaları
                    var itemSubTotal = item.Quantity * item.UnitPrice;
                    var discountAmount = itemSubTotal * (item.DiscountRate / 100);
                    var afterDiscount = itemSubTotal - discountAmount;
                    var itemTax = afterDiscount * (item.TaxRate / 100);
                    var itemTotal = afterDiscount + itemTax;

                    subTotal += afterDiscount;
                    taxAmount += itemTax;

                    saleItems.Add(new SaleItem
                    {
                        StockId = item.StockId,
                        ProductName = stock.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        DiscountRate = item.DiscountRate,
                        TotalPrice = itemTotal
                    });

                    // Stok düş
                    stock.Quantity -= item.Quantity;
                    stock.UpdatedDate = DateTime.UtcNow;
                }

                var totalAmount = subTotal + taxAmount - request.DiscountAmount;

                // Satış oluştur
                var sale = new Sale
                {
                    UserId = userId,
                    SaleNumber = saleNumber,
                    SaleDate = request.SaleDate,
                    CustomerName = request.CustomerName,
                    CustomerPhone = request.CustomerPhone,
                    CustomerEmail = request.CustomerEmail,
                    CustomerAddress = request.CustomerAddress,
                    CustomerTaxNumber = request.CustomerTaxNumber,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = request.DiscountAmount,
                    TotalAmount = totalAmount,
                    PaymentStatus = PaymentStatus.Pending,
                    Notes = request.Notes,
                    CreatedDate = DateTime.UtcNow,
                    SaleItems = saleItems
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetSaleById(sale.Id, userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SaleDto>> GetSales(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(s => s.SaleDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.SaleDate <= endDate.Value);
            }

            var sales = await query
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return sales.Select(MapToDto).ToList();
        }

        public async Task<SaleDto> GetSaleById(int id, int userId)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (sale == null)
            {
                throw new Exception("Satış bulunamadı");
            }

            return MapToDto(sale);
        }

        public async Task<SaleDto> GetSaleByNumber(string saleNumber, int userId)
        {
            var sale = await _context.Sales
                .Include(s => s.SaleItems)
                .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber && s.UserId == userId);

            if (sale == null)
            {
                throw new Exception("Satış bulunamadı");
            }

            return MapToDto(sale);
        }

        public async Task<bool> CancelSale(int id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sale = await _context.Sales
                    .Include(s => s.SaleItems)
                    .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

                if (sale == null)
                {
                    return false;
                }

                if (sale.PaymentStatus == PaymentStatus.Paid)
                {
                    throw new Exception("Ödemesi tamamlanmış satışlar iptal edilemez");
                }

                // Stokları geri ekle
                foreach (var item in sale.SaleItems)
                {
                    var stock = await _context.Stocks.FindAsync(item.StockId);
                    if (stock != null)
                    {
                        stock.Quantity += item.Quantity;
                        stock.UpdatedDate = DateTime.UtcNow;
                    }
                }

                sale.PaymentStatus = PaymentStatus.Cancelled;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<SaleDto>> GetPendingPaymentSales(int userId)
        {
            var sales = await _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.UserId == userId &&
                           (s.PaymentStatus == PaymentStatus.Pending ||
                            s.PaymentStatus == PaymentStatus.PartialPaid))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return sales.Select(MapToDto).ToList();
        }

        public async Task<bool> UpdateSalePaymentStatus(int saleId, int userId)
        {
            var sale = await _context.Sales
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.UserId == userId);

            if (sale == null)
            {
                return false;
            }

            var totalPaid = sale.Payments
                .Where(p => p.PaymentType == PaymentType.Income)
                .Sum(p => p.Amount);

            if (totalPaid >= sale.TotalAmount)
            {
                sale.PaymentStatus = PaymentStatus.Paid;
            }
            else if (totalPaid > 0)
            {
                sale.PaymentStatus = PaymentStatus.PartialPaid;
            }
            else
            {
                sale.PaymentStatus = PaymentStatus.Pending;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #region Private Methods

        private async Task<string> GenerateSaleNumber(int userId)
        {
            var today = DateTime.Today;
            var prefix = $"S{today:yyyyMMdd}";

            var lastSale = await _context.Sales
                .Where(s => s.UserId == userId && s.SaleNumber.StartsWith(prefix))
                .OrderByDescending(s => s.SaleNumber)
                .FirstOrDefaultAsync();

            if (lastSale == null)
            {
                return $"{prefix}0001";
            }

            var lastNumber = int.Parse(lastSale.SaleNumber.Substring(prefix.Length));
            return $"{prefix}{(lastNumber + 1):D4}";
        }

        private SaleDto MapToDto(Sale sale)
        {
            return new SaleDto
            {
                Id = sale.Id,
                SaleNumber = sale.SaleNumber,
                SaleDate = sale.SaleDate,
                CustomerName = sale.CustomerName,
                CustomerPhone = sale.CustomerPhone,
                CustomerEmail = sale.CustomerEmail,
                CustomerAddress = sale.CustomerAddress,
                CustomerTaxNumber = sale.CustomerTaxNumber,
                SubTotal = sale.SubTotal,
                TaxAmount = sale.TaxAmount,
                DiscountAmount = sale.DiscountAmount,
                TotalAmount = sale.TotalAmount,
                PaymentStatus = sale.PaymentStatus,
                Notes = sale.Notes,
                CreatedDate = sale.CreatedDate,
                Items = sale.SaleItems?.Select(si => new SaleItemDto
                {
                    Id = si.Id,
                    StockId = si.StockId,
                    ProductName = si.ProductName,
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    TaxRate = si.TaxRate,
                    DiscountRate = si.DiscountRate,
                    TotalPrice = si.TotalPrice
                }).ToList() ?? new List<SaleItemDto>()
            };
        }

        #endregion
    }
}