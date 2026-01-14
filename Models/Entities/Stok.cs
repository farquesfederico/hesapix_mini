namespace Hesapix.Models.Entities
{
    public class Stock
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; } // Adet, Kg, Litre vb.
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal? MinimumStock { get; set; }
        public string? Barcode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; }

       /* public decimal TaxRate { get; set; } // KDV oranı */

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual ICollection<SaleItem> SaleItems { get; set; }
    }
}