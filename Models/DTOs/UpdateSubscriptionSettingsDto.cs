namespace Hesapix.Models.DTOs;

public class UpdateSubscriptionSettingsDto
{
    public bool TrialEnabled { get; set; }
    public int TrialDurationDays { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public bool CampaignEnabled { get; set; }
    public decimal CampaignDiscountPercent { get; set; }
}
