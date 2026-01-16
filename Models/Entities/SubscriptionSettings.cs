namespace Hesapix.Models.Entities;

public class SubscriptionSettings
{
    public int Id { get; set; }

    public bool TrialEnabled { get; set; }
    public int TrialDurationDays { get; set; }

    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }

    public bool CampaignEnabled { get; set; }
    public decimal CampaignDiscountPercent { get; set; }

    public DateTime UpdatedAt { get; set; }
}
