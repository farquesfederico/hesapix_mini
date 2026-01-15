using AutoMapper;
using Hesapix.Data;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Models.Entities;
using Hesapix.Models.Enums;
using Hesapix.Services.Interfaces;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
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

    public SubscriptionService(
        ApplicationDbContext context,
        IMapper mapper,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message, object? Data)> CreateSubscriptionAsync(int userId, CreateSubscriptionRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "Kullanıcı bulunamadı", null);
        }

        // Trial kontrolü
        var settings = _configuration.GetSection("SubscriptionSettings");
        var trialEnabled = settings.GetValue<bool>("TrialEnabled");
        var hasTrialUsed = await _context.Subscriptions.AnyAsync(s => s.UserId == userId && s.IsTrial);

        bool isTrial = trialEnabled && !hasTrialUsed;

        // Fiyat hesaplama
        decimal price = await GetSubscriptionPriceAsync(request);
        decimal finalPrice = isTrial ? 0 : price;

        if (isTrial)
        {
            // Trial subscription oluştur
            var trialDays = settings.GetValue<int>("TrialDurationDays");
            var subscription = new Subscription
            {
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Trial,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(trialDays),
                Price = price,
                FinalPrice = 0,
                IsTrial = true,
                AutoRenew = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return (true, $"Ücretsiz deneme aboneliğiniz başladı. {trialDays} gün geçerlidir.",
                _mapper.Map<SubscriptionDto>(subscription));
        }

        // Ödeme gateway'e göre işlem
        if (request.PaymentGateway == "Iyzico")
        {
            var paymentResult = await ProcessIyzicoPaymentAsync(user, request, finalPrice);
            if (!paymentResult.Success)
            {
                return (false, paymentResult.Message, null);
            }

            var subscription = new Subscription
            {
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = request.PlanType == SubscriptionPlanType.Monthly
                    ? DateTime.UtcNow.AddMonths(1)
                    : DateTime.UtcNow.AddYears(1),
                Price = price,
                FinalPrice = finalPrice,
                IsTrial = false,
                AutoRenew = true,
                PaymentGateway = "Iyzico",
                PaymentTransactionId = paymentResult.TransactionId,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            await _emailService.SendSubscriptionConfirmationEmailAsync(
                user.Email, user.CompanyName, subscription.EndDate);

            return (true, "Aboneliğiniz başarıyla oluşturuldu", _mapper.Map<SubscriptionDto>(subscription));
        }

        return (false, "Desteklenmeyen ödeme yöntemi", null);
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                && s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return subscription != null ? _mapper.Map<SubscriptionDto>(subscription) : null;
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .AnyAsync(s => s.UserId == userId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                && s.EndDate > DateTime.UtcNow);
    }

    public async Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId)
    {
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return (false, "Aktif abonelik bulunamadı");
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.AutoRenew = false;
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Aboneliğiniz iptal edildi");
    }

    public async Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload)
    {
        // Iyzico webhook işlemleri
        Log.Information("Iyzico webhook received: {Payload}", payload);
        // TODO: Implement webhook handling
        return (true, "Webhook processed");
    }

    public async Task ProcessExpiredSubscriptionsAsync()
    {
        var expiredSubscriptions = await _context.Subscriptions
            .Include(s => s.User)
            .Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial)
                && s.EndDate <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = SubscriptionStatus.Expired;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        Log.Information("Processed {Count} expired subscriptions", expiredSubscriptions.Count);
    }

    public async Task<decimal> GetSubscriptionPriceAsync(CreateSubscriptionRequest request)
    {
        var settings = _configuration.GetSection("SubscriptionSettings");

        decimal basePrice = request.PlanType == SubscriptionPlanType.Monthly
            ? settings.GetValue<decimal>("MonthlyPrice")
            : settings.GetValue<decimal>("YearlyPrice");

        var campaignEnabled = settings.GetValue<bool>("CampaignEnabled");
        if (campaignEnabled)
        {
            var discount = settings.GetValue<decimal>("CampaignDiscountPercent");
            basePrice -= basePrice * (discount / 100);
        }

        return basePrice;
    }

    private async Task<(bool Success, string Message, string? TransactionId)> ProcessIyzicoPaymentAsync(
        User user, CreateSubscriptionRequest request, decimal amount)
    {
        try
        {
            var iyzicoSettings = _configuration.GetSection("IyzicoSettings");
            var options = new Options
            {
                ApiKey = iyzicoSettings["ApiKey"],
                SecretKey = iyzicoSettings["SecretKey"],
                BaseUrl = iyzicoSettings["BaseUrl"]
            };

            // Bu örnek basitleştirilmiş. Gerçek implementasyonda kart bilgileri alınmalı
            // TODO: Frontend'den kart bilgilerini al ve işle

            return (true, "Ödeme başarılı", Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Iyzico payment failed for user {UserId}", user.Id);
            return (false, "Ödeme işlemi başarısız oldu", null);
        }
    }
}