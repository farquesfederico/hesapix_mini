using System.ComponentModel.DataAnnotations;
using Hesapix.Models.Enums; // PaymentMethod enum'u burada olmalı

namespace Hesapix.Models.DTOs.Sale
{
    public class CreateSaleRequest
    {
        [Required]
        public DateTime? SaleDate { get; set; }

        [Required(ErrorMessage = "Müşteri adı zorunludur")]
        [MaxLength(200)]
        public string CustomerName { get; set; }

        [Phone]
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? CustomerEmail { get; set; }

        [MaxLength(500)]
        public string? CustomerAddress { get; set; }

        [MaxLength(50)]
        public string? CustomerTaxNumber { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "En az bir ürün eklenmelidir")]
        public List<SaleItemRequest> Items { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // --------- EKLENECEK ALANLAR ---------
        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; } // Ödenen miktar

        public PaymentMethod PaymentMethod { get; set; } // Enum tipinde ödeme yöntemi
    }

    public class SaleItemRequest
    {
        [Required]
        public int StokId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, 100)]
        public decimal TaxRate { get; set; }

        [Range(0, 100)]
        public decimal DiscountRate { get; set; }
    }
}
