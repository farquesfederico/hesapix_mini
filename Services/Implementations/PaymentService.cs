using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<PaymentDto>>> GetPaymentsByUserIdAsync(
            int userId,
            DateTime? startDate,
            DateTime? endDate,
            int page,
            int pageSize)
        {
            try
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

                var totalCount = await query.CountAsync();

                var payments = await query
                    .OrderByDescending(p => p.PaymentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking()
                    .ToListAsync();

                var paymentDtos = _mapper.Map<List<PaymentDto>>(payments);

                return ApiResponse<List<PaymentDto>>.SuccessResult(paymentDtos, $"Toplam {totalCount} ödeme bulundu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödemeler listeleme hatası");
                return ApiResponse<List<PaymentDto>>.FailResult("Ödemeler listelenemedi");
            }
        }

        public async Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(int paymentId, int userId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Sale)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

                if (payment == null)
                {
                    return ApiResponse<PaymentDto>.FailResult("Ödeme bulunamadı veya erişim yetkiniz yok");
                }

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return ApiResponse<PaymentDto>.SuccessResult(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme bilgisi alma hatası");
                return ApiResponse<PaymentDto>.FailResult("Ödeme bilgisi alınamadı");
            }
        }

        public async Task<ApiResponse<PaymentDto>> CreatePaymentAsync(CreatePaymentRequest request, int userId)
        {
            try
            {
                var sale = await _context.Sales
                    .FirstOrDefaultAsync(s => s.Id == request.SaleId && s.UserId == userId);

                if (sale == null)
                {
                    return ApiResponse<PaymentDto>.FailResult("Satış bulunamadı veya erişim yetkiniz yok");
                }

                if (request.Amount <= 0)
                {
                    return ApiResponse<PaymentDto>.FailResult("Ödeme tutarı sıfırdan büyük olmalıdır");
                }

                if (request.Amount > sale.RemainingAmount)
                {
                    return ApiResponse<PaymentDto>.FailResult($"Ödeme tutarı kalan borçtan ({sale.RemainingAmount:C}) fazla olamaz");
                }

                var payment = new Payment
                {
                    UserId = userId,
                    SaleId = request.SaleId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentDate = request.PaymentDate.HasValue ? request.PaymentDate.Value : DateTime.UtcNow,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                sale.PaidAmount += request.Amount;
                sale.RemainingAmount -= request.Amount;

                await _context.SaveChangesAsync();

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return ApiResponse<PaymentDto>.SuccessResult(paymentDto, "Ödeme kaydedildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme oluşturma hatası");
                return ApiResponse<PaymentDto>.FailResult("Ödeme oluşturulamadı");
            }
        }

        public async Task<ApiResponse<PaymentDto>> UpdatePaymentAsync(int paymentId, CreatePaymentRequest request, int userId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Sale)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

                if (payment == null)
                {
                    return ApiResponse<PaymentDto>.FailResult("Ödeme bulunamadı veya erişim yetkiniz yok");
                }

                var sale = payment.Sale;
                if (sale == null)
                {
                    return ApiResponse<PaymentDto>.FailResult("İlgili satış bulunamadı");
                }

                sale.PaidAmount -= payment.Amount;
                sale.RemainingAmount += payment.Amount;

                if (request.Amount > sale.RemainingAmount)
                {
                    return ApiResponse<PaymentDto>.FailResult($"Ödeme tutarı kalan borçtan ({sale.RemainingAmount:C}) fazla olamaz");
                }

                payment.Amount = request.Amount;
                payment.PaymentMethod = request.PaymentMethod;
                payment.PaymentDate = request.PaymentDate.HasValue ? request.PaymentDate.Value : payment.PaymentDate;
                payment.Notes = request.Notes;
                payment.UpdatedAt = DateTime.UtcNow;

                sale.PaidAmount += request.Amount;
                sale.RemainingAmount -= request.Amount;

                await _context.SaveChangesAsync();

                var paymentDto = _mapper.Map<PaymentDto>(payment);
                return ApiResponse<PaymentDto>.SuccessResult(paymentDto, "Ödeme güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme güncelleme hatası");
                return ApiResponse<PaymentDto>.FailResult("Ödeme güncellenemedi");
            }
        }

        public async Task<ApiResponse<bool>> DeletePaymentAsync(int paymentId, int userId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Sale)
                    .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

                if (payment == null)
                {
                    return ApiResponse<bool>.FailResult("Ödeme bulunamadı veya erişim yetkiniz yok");
                }

                var sale = payment.Sale;
                if (sale != null)
                {
                    sale.PaidAmount -= payment.Amount;
                    sale.RemainingAmount += payment.Amount;
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Ödeme silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme silme hatası");
                return ApiResponse<bool>.FailResult("Ödeme silinemedi");
            }
        }

        public async Task<ApiResponse<List<PaymentDto>>> GetPaymentsBySaleIdAsync(int saleId, int userId)
        {
            try
            {
                var saleExists = await _context.Sales
                    .AnyAsync(s => s.Id == saleId && s.UserId == userId);

                if (!saleExists)
                {
                    return ApiResponse<List<PaymentDto>>.FailResult("Satış bulunamadı veya erişim yetkiniz yok");
                }

                var payments = await _context.Payments
                    .Include(p => p.Sale)
                    .Where(p => p.SaleId == saleId && p.UserId == userId)
                    .OrderByDescending(p => p.PaymentDate)
                    .AsNoTracking()
                    .ToListAsync();

                var paymentDtos = _mapper.Map<List<PaymentDto>>(payments);
                return ApiResponse<List<PaymentDto>>.SuccessResult(paymentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satış ödemeleri listeleme hatası");
                return ApiResponse<List<PaymentDto>>.FailResult("Satış ödemeleri listelenemedi");
            }
        }
    }
}