namespace Hesapix.Models.Enums;

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    PaymentFailed = 4
}

public enum SubscriptionPlanType
{
    Monthly = 0,
    Yearly = 1
}