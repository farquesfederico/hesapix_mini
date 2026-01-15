using System.ComponentModel.DataAnnotations;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums; 
namespace Hesapix.Models.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        public int? SaleId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required(ErrorMessage = "Müşteri/Tedarikçi adı zorunludur")]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Tutar 0'dan büyük olmalıdır")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentType PaymentType { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(50)]
        public string? CheckNumber { get; set; }

        public DateTime? CheckDate { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}