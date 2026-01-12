namespace Hesapix.Models.Entities
{
    public class Sale
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string SaleNumber { get; set; } // Otomatik oluşturulan satış numarası
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTaxNumber { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }

    public enum PaymentStatus
    {
        Pending = 1,      // Beklemede
        PartialPaid = 2,  // Kısmi Ödeme
        Paid = 3,         // Ödendi
        Cancelled = 4     // İptal
    }
}