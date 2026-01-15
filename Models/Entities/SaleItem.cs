namespace Hesapix.Models.Entities
{
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int StokId { get; set; }
        public string ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal TotalPrice { get; set; }

        // Navigation Properties
        public virtual Sale Sale { get; set; }
        public virtual Stok Stock { get; set; }
    }
}