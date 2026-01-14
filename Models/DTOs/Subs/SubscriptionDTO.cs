using Hesapix.Models.Entities;

namespace Hesapix.Models.DTOs.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? UserEmail { get; set; }
        public SubscriptionType Type { get; set; }
        public SubscriptionPlatform Platform { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
    }
}
