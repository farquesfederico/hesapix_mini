namespace Hesapix.Models.Entities
{
    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public SubscriptionType Type { get; set; }
        public SubscriptionPlatform Platform { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? ReceiptData { get; set; }

        // Navigation Properties
        public virtual User User { get; set; }
    }

    public enum SubscriptionType
    {
        Monthly = 1,
        Yearly = 2
    }

    public enum SubscriptionPlatform
    {
        Web = 1,
        GooglePlay = 2,
        AppStore = 3,
        MailOrder = 4
    }
}