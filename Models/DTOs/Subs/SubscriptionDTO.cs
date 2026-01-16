using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Subscription;

public class SubscriptionDto
{
    public int Id { get; set; }
    public SubscriptionPlanType PlanType { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsTrial { get; set; }
    public bool AutoRenew { get; set; }
    public bool WillCancelAtPeriodEnd { get; set; } // ← YENİ!
    public DateTime? CancelledAt { get; set; } // ← YENİ!
    public decimal Price { get; set; }
    public decimal FinalPrice { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsActive { get; set; }
    public string StatusMessage { get; set; } = string.Empty; // ← YENİ! Kullanıcıya mesaj
}