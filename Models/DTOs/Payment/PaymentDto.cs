using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
namespace Hesapix.Models.DTOs.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public string PaymentTypeText { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodText { get; set; }
        public string? CheckNumber { get; set; }
        public DateTime? CheckDate { get; set; }
        public string? BankName { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}