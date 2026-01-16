using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Iyzipay.Model.V2.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Hesapix.Services.Implementations;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IMobilePaymentService _mobilePaymentService;

    public SubscriptionService(
        ApplicationDbContext context,
        IMapper mapper,
        IConfiguration configuration,
        IEmailService emailService,
        IMobilePaymentService mobilePaymentService)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
        _emailService = emailService;
        _mobilePaymentService = mobilePaymentService;
    }

    // -------------------------------------------------
    // PAYMENT INIT
    // -------------------------------------------------
    public async Task<(bool Success, string Message, object? Data)>
        InitiatePaymentAsync(int userId, CreateSubscriptionRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı", null);

        if (await HasActiveSubscriptionAsync(userId))
            return (false, "Zaten aktif aboneliğiniz var", null);

        var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();
        if (settings == null)
            return (false, "Abonelik ayarları tanımlı değil", null);

        if (settings.TrialEnabled)
        {
            var hasTrial = await _context.Subscriptions
                .AnyAsync(x => x.UserId == userId && x.IsTrial);

            if (!hasTrial)
            {
                var trialResult = await ActivateTrialAsync(userId);
                return (trialResult.Success, trialResult.Message, null);
            }
        }

        var price = CalculatePrice(request, settings);

        return request.PaymentGateway switch
        {
            PaymentGateway.Iyzico =>
                (true, "Ödeme başlatıldı",
                    new { PaymentUrl = $"https://iyzico.fake/pay?amount={price}", Amount = price }),

            PaymentGateway.GooglePlay =>
                await ProcessGooglePlayPurchaseAsync(userId, request),

            PaymentGateway.AppStore =>
                await ProcessAppStorePurchaseAsync(userId, request),

            _ => (false, "Desteklenmeyen ödeme yöntemi", null)
        };
    }

    // -------------------------------------------------
    // COMPLETE PAYMENT
    // -------------------------------------------------
    public async Task<(bool Success, string Message)>
        CompletePaymentAsync(int userId, CreateSubscriptionRequest request, string transactionId)
    {
        var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();
        if (settings == null)
            return (false, "Abonelik ayarları bulunamadı");

        var price = CalculatePrice(request, settings);

        var subscription = new Hesapix.Models.Entities.Subscription
        {
            UserId = userId,
            PlanType = request.PlanType,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = request.PlanType == SubscriptionPlanType.Monthly
                ? DateTime.UtcNow.AddMonths(1)
                : DateTime.UtcNow.AddYears(1),
            Price = price,
            FinalPrice = price,
            IsTrial = false,
            AutoRenew = true,
            PaymentGateway = request.PaymentGateway,
            PaymentTransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return (true, "Abonelik başarıyla oluşturuldu");
    }

    // -------------------------------------------------
    // TRIAL
    // -------------------------------------------------
    public async Task<(bool Success, string Message)> ActivateTrialAsync(int userId)
    {
        var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();
        if (settings == null || !settings.TrialEnabled)
            return (false, "Trial aktif değil");

        var used = await _context.Subscriptions
            .AnyAsync(x => x.UserId == userId && x.IsTrial);

        if (used)
            return (false, "Trial daha önce kullanılmış");

        var subscription = new Hesapix.Models.Entities.Subscription
        {
            UserId = userId,
            PlanType = SubscriptionPlanType.Monthly,
            Status = SubscriptionStatus.Trial,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(settings.TrialDurationDays),
            IsTrial = true,
            AutoRenew = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return (true, $"Trial başladı ({settings.TrialDurationDays} gün)");
    }

    // -------------------------------------------------
    // READ
    // -------------------------------------------------
    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId)
    {
        var sub = await _context.Subscriptions
            .Where(x => x.UserId == userId &&
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
                x.EndDate > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        return sub == null ? null : _mapper.Map<SubscriptionDto>(sub);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions.AnyAsync(x =>
            x.UserId == userId &&
            (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
            x.EndDate > DateTime.UtcNow);
    }

    // -------------------------------------------------
    // CANCEL / REACTIVATE
    // -------------------------------------------------
    public async Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId)
    {
        var sub = await _context.Subscriptions
            .Where(x => x.UserId == userId &&
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub == null)
            return (false, "Aktif abonelik bulunamadı");

        sub.WillCancelAtPeriodEnd = true;
        sub.AutoRenew = false;
        sub.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Abonelik iptal edildi");
    }

    public async Task<(bool Success, string Message)> ReactivateSubscriptionAsync(int userId)
    {
        var sub = await _context.Subscriptions
            .Where(x => x.UserId == userId && x.WillCancelAtPeriodEnd)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub == null)
            return (false, "İptal edilmiş abonelik yok");

        sub.WillCancelAtPeriodEnd = false;
        sub.AutoRenew = true;
        sub.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Abonelik yeniden aktif edildi");
    }

    // -------------------------------------------------
    // PLANS
    // -------------------------------------------------
    public async Task<object> GetAvailablePlansAsync()
    {
        var s = await _context.SubscriptionSettings.FirstOrDefaultAsync();
        if (s == null) return new { };

        var plans = new List<object>();

        // 🎁 Trial Plan
        if (s.TrialEnabled)
        {
            plans.Add(new
            {
                Type = "Deneme Sürümü",
                Price = "Ücretsiz",
                DurationDays = s.TrialDurationDays
            });
        }

        // 💳 Aylık Plan
        plans.Add(new
        {
            Type = "Aylık",
            Price = s.MonthlyPrice,
            Duration = "1 Ay"
        });

        // 💳 Yıllık Plan
        plans.Add(new
        {
            Type = "Yıllık",
            Price = s.YearlyPrice,
            Duration = "1 Yıl"
        });

        return new
        {
            Plans = plans,

            Trial = new
            {
                Enabled = s.TrialEnabled,
                DurationDays = s.TrialDurationDays
            },

            Campaign = new
            {
                Enabled = s.CampaignEnabled,
                DiscountPercent = s.CampaignDiscountPercent
            }
        };
    }

    // -------------------------------------------------
    // EXPIRE
    // -------------------------------------------------
    public async Task ProcessExpiredSubscriptionsAsync()
    {
        var expired = await _context.Subscriptions
            .Where(x => x.EndDate <= DateTime.UtcNow &&
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial))
            .ToListAsync();

        foreach (var s in expired)
        {
            s.Status = s.WillCancelAtPeriodEnd
                ? SubscriptionStatus.Cancelled
                : SubscriptionStatus.Expired;
            s.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    // -------------------------------------------------
    // WEBHOOK
    // -------------------------------------------------
    public async Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload)
    {
        Log.Information("Iyzico Webhook: {Payload}", payload);
        return (true, "Webhook alındı");
    }

    // -------------------------------------------------
    // PRIVATE HELPERS
    // -------------------------------------------------
    private decimal CalculatePrice(CreateSubscriptionRequest r, SubscriptionSettings s)
    {
        var price = r.PlanType == SubscriptionPlanType.Monthly
            ? s.MonthlyPrice
            : s.YearlyPrice;

        if (s.CampaignEnabled)
            price -= price * (s.CampaignDiscountPercent / 100);

        return price;
    }

    private async Task<(bool, string, object?)>
        ProcessGooglePlayPurchaseAsync(int userId, CreateSubscriptionRequest request)
    {
        return (false, "Google Play henüz aktif değil", null);
    }

    private async Task<(bool, string, object?)>
        ProcessAppStorePurchaseAsync(int userId, CreateSubscriptionRequest request)
    {
        return (false, "App Store henüz aktif değil", null);
    }
}
