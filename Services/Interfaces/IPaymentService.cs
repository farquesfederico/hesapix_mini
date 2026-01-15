using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Enums;

namespace Hesapix.Services.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(int userId, int pageNumber = 1, int pageSize = 10, PaymentType? type = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<PaymentDto?> GetPaymentByIdAsync(int id, int userId);
    Task<(bool Success, string Message, PaymentDto? Data)> CreatePaymentAsync(CreatePaymentRequest request, int userId);
    Task<(bool Success, string Message, PaymentDto? Data)> UpdatePaymentAsync(int id, CreatePaymentRequest request, int userId);
    Task<(bool Success, string Message)> DeletePaymentAsync(int id, int userId);
}