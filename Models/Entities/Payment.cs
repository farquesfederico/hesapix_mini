namespace Hesapix.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SaleId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; }
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? CheckNumber { get; set; }
        public DateTime? CheckDate { get; set; }
        public string? BankName { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
        public virtual Sale Sale { get; set; }
    }

    public enum PaymentType
    {
        Income = 1,   // Tahsilat
        Expense = 2   // Ödeme
    }

    public enum PaymentMethod
    {
        Cash = 1,           // Nakit
        CreditCard = 2,     // Kredi Kartı
        BankTransfer = 3,   // Havale/EFT
        Check = 4,          // Çek
        Other = 5           // Diğer
    }
}