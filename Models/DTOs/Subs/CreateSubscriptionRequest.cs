using Hesapix.Models.Entities;

namespace Hesapix.Models.DTOs.Subscription
{
    public class CreateSubscriptionRequest
    {
        public int UserId { get; set; }                 // Hangi kullanıcıya abonelik verilecek
        public SubscriptionType Type { get; set; }
        public SubscriptionPlatform Platform { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? ReceiptData { get; set; }
        public DateTime? StartDate { get; set; }        // Opsiyonel, admin belirleyebilir
        public DateTime? EndDate { get; set; }          // Opsiyonel, admin belirleyebilir
    }
}
