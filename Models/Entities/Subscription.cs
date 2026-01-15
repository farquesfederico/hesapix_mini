using Hesapix.Models.Enums;

namespace Hesapix.Models.Entities;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public SubscriptionPlanType PlanType { get; set; }
    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }

    public bool IsTrial { get; set; } = false;
    public bool AutoRenew { get; set; } = true;

    // Payment Gateway Info
    public string? PaymentGateway { get; set; } // "Iyzico", "GooglePlay", "AppStore"
    public string? PaymentTransactionId { get; set; }
    public string? PaymentToken { get; set; }
    public DateTime? PaymentDate { get; set; }

    // Retry Mechanism
    public int PaymentRetryCount { get; set; } = 0;
    public DateTime? LastPaymentRetryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}