using System.ComponentModel.DataAnnotations;

namespace Hesapix.Models.DTOs.Stock
{
    public class StockDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün kodu zorunludur")]
        [MaxLength(50)]
        public string ProductCode { get; set; }

        [Required(ErrorMessage = "Ürün adı zorunludur")]
        [MaxLength(200)]
        public string ProductName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Miktar 0'dan büyük olmalıdır")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Alış fiyatı 0'dan büyük olmalıdır")]
        public decimal PurchasePrice { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Satış fiyatı 0'dan büyük olmalıdır")]
        public decimal SalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinimumStock { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }
    }
}