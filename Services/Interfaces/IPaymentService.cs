using Hesapix.Models.DTOs.Payment;
using Hesapix.Models.Entities;

namespace Hesapix.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreatePayment(CreatePaymentRequest request, int userId);
        Task<List<PaymentDto>> GetPayments(int userId, int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null);
        Task<PaymentDto> GetPaymentById(int id, int userId);
        Task<List<PaymentDto>> GetPaymentsBySaleId(int saleId, int userId);
        Task<List<PaymentDto>> GetPaymentsByType(PaymentType type, int userId);
        Task<bool> DeletePayment(int id, int userId);
    }
}