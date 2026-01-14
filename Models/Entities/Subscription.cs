namespace Hesapix.Models.Entities
{
    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public SubscriptionType Type { get; set; }        // Aylık / Yıllık
        public SubscriptionPlatform Platform { get; set; } // Web / GooglePlay / AppStore
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }       // Online ödeme ID
        public string? ReceiptData { get; set; }        // Ödeme makbuzu / fatura

        // Navigation
        public virtual User User { get; set; }
    }

    public enum SubscriptionType
    {
        Monthly = 1,
        Yearly = 2,
        Unlimited = 3      // Admin sınırsız abonelik
    }

    public enum SubscriptionPlatform
    {
        Web = 1,
        GooglePlay = 2,
        AppStore = 3,
        MailOrder = 4
    }
}
