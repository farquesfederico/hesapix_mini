using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Subscription;

public class CreateSubscriptionRequest
{
    public SubscriptionPlanType PlanType { get; set; }
    public string PaymentGateway { get; set; } = "Iyzico"; // "Iyzico", "GooglePlay", "AppStore"
    public string? CampaignCode { get; set; }
}