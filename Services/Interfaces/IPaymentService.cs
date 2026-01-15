using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Payment;

namespace Hesapix.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<List<PaymentDto>>> GetPaymentsByUserIdAsync(int userId, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(int paymentId, int userId);
        Task<ApiResponse<PaymentDto>> CreatePaymentAsync(CreatePaymentRequest request, int userId);
        Task<ApiResponse<PaymentDto>> UpdatePaymentAsync(int paymentId, CreatePaymentRequest request, int userId);
        Task<ApiResponse<bool>> DeletePaymentAsync(int paymentId, int userId);
        Task<ApiResponse<List<PaymentDto>>> GetPaymentsBySaleIdAsync(int saleId, int userId);
    }
}