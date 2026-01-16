using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Subscription;

public class CreateSubscriptionRequest
{
    public SubscriptionPlanType PlanType { get; set; }
    public PaymentGateway PaymentGateway { get; set; } = PaymentGateway.Iyzico;
    public string? CampaignCode { get; set; }

    // Google Play specific
    public string? GooglePlayPurchaseToken { get; set; }
    public string? GooglePlayProductId { get; set; }

    // App Store specific
    public string? AppStoreReceiptData { get; set; }
    public string? AppStoreTransactionId { get; set; }
}