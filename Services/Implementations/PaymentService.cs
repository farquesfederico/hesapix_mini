using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public PaymentService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(int userId, int pageNumber = 1, int pageSize = 10,
        PaymentType? type = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Payments.Where(p => p.UserId == userId);

        if (type.HasValue)
            query = query.Where(p => p.PaymentType == type.Value);

        if (startDate.HasValue)
            query = query.Where(p => p.PaymentDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.PaymentDate <= endDate.Value);

        var totalCount = await query.CountAsync();

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PaymentDto>
        {
            Items = _mapper.Map<List<PaymentDto>>(payments),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(int id, int userId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
    }

    public async Task<(bool Success, string Message, PaymentDto? Data)> CreatePaymentAsync(CreatePaymentRequest request, int userId)
    {
        var payment = _mapper.Map<Payment>(request);
        payment.UserId = userId;
        payment.CreatedAt = DateTime.UtcNow;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var createdPayment = _mapper.Map<PaymentDto>(payment);
        return (true, "Ödeme başarıyla oluşturuldu", createdPayment);
    }

    public async Task<(bool Success, string Message, PaymentDto? Data)> UpdatePaymentAsync(int id, CreatePaymentRequest request, int userId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (payment == null)
        {
            return (false, "Ödeme bulunamadı", null);
        }

        payment.CustomerName = request.CustomerName;
        payment.PaymentType = request.PaymentType;
        payment.PaymentMethod = request.PaymentMethod;
        payment.Amount = request.Amount;
        payment.PaymentDate = request.PaymentDate;
        payment.Description = request.Description;
        payment.InvoiceNumber = request.InvoiceNumber;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updatedPayment = _mapper.Map<PaymentDto>(payment);
        return (true, "Ödeme başarıyla güncellendi", updatedPayment);
    }

    public async Task<(bool Success, string Message)> DeletePaymentAsync(int id, int userId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (payment == null)
        {
            return (false, "Ödeme bulunamadı");
        }

        // Soft delete
        payment.IsDeleted = true;
        payment.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Ödeme başarıyla silindi");
    }
}