using Microsoft.EntityFrameworkCore;
using Hesapix.Data;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISaleService _saleService;

        public PaymentService(ApplicationDbContext context, ISaleService saleService)
        {
            _context = context;
            _saleService = saleService;
        }

        public async Task<PaymentDto> CreatePayment(CreatePaymentRequest request, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payment = new Payment
                {
                    UserId = userId,
                    SaleId = request.SaleId,
                    PaymentDate = request.PaymentDate,
                    CustomerName = request.CustomerName,
                    Amount = request.Amount,
                    PaymentType = request.PaymentType,
                    PaymentMethod = request.PaymentMethod,
                    CheckNumber = request.CheckNumber,
                    CheckDate = request.CheckDate,
                    BankName = request.BankName,
                    ReferenceNumber = request.ReferenceNumber,
                    Notes = request.Notes,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Eğer ödeme bir satışa bağlıysa, satışın ödeme durumunu güncelle
                if (request.SaleId.HasValue && request.PaymentType == PaymentType.Income)
                {
                    await _saleService.UpdateSalePaymentStatus(request.SaleId.Value, userId);
                }

                await transaction.CommitAsync();

                return await GetPaymentById(payment.Id, userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PaymentDto>> GetPayments(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments
                .Include(p => p.Sale)
                .Where(p => p.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<PaymentDto> GetPaymentById(int id, int userId)
        {
            var payment = await _context.Payments
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (payment == null)
            {
                throw new Exception("Ödeme kaydı bulunamadı");
            }

            return MapToDto(payment);
        }

        public async Task<List<PaymentDto>> GetPaymentsBySaleId(int saleId, int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.Sale)
                .Where(p => p.SaleId == saleId && p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<List<PaymentDto>> GetPaymentsByType(PaymentType type, int userId)
        {
            var payments = await _context.Payments
                .Include(p => p.Sale)
                .Where(p => p.PaymentType == type && p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<bool> DeletePayment(int id, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                if (payment == null)
                {
                    return false;
                }

                var saleId = payment.SaleId;

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                // Eğer ödeme bir satışa bağlıysa, satışın ödeme durumunu güncelle
                if (saleId.HasValue)
                {
                    await _saleService.UpdateSalePaymentStatus(saleId.Value, userId);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #region Private Methods

        private PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                SaleId = payment.SaleId,
                SaleNumber = payment.Sale?.SaleNumber,
                PaymentDate = payment.PaymentDate,
                CustomerName = payment.CustomerName,
                Amount = payment.Amount,
                PaymentType = payment.PaymentType,
                PaymentTypeText = GetPaymentTypeText(payment.PaymentType),
                PaymentMethod = payment.PaymentMethod,
                PaymentMethodText = GetPaymentMethodText(payment.PaymentMethod),
                CheckNumber = payment.CheckNumber,
                CheckDate = payment.CheckDate,
                BankName = payment.BankName,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes,
                CreatedDate = payment.CreatedDate
            };
        }

        private string GetPaymentTypeText(PaymentType type)
        {
            return type switch
            {
                PaymentType.Income => "Tahsilat",
                PaymentType.Expense => "Ödeme",
                _ => "Bilinmiyor"
            };
        }

        private string GetPaymentMethodText(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Nakit",
                PaymentMethod.CreditCard => "Kredi Kartı",
                PaymentMethod.BankTransfer => "Havale/EFT",
                PaymentMethod.Check => "Çek",
                PaymentMethod.Other => "Diğer",
                _ => "Bilinmiyor"
            };
        }

        #endregion
    }
}