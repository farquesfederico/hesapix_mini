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
    private readonly IMobilePaymentService _mobilePaymentService;
    private readonly Options _iyzicoOptions;

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

        // Iyzico Options
        _iyzicoOptions = new Options
        {
            ApiKey = configuration["Iyzico:ApiKey"],
            SecretKey = configuration["Iyzico:SecretKey"],
            BaseUrl = configuration["Iyzico:BaseUrl"]
        };
    }

    // ========================================
    // PAYMENT INIT
    // ========================================
    public async Task<(bool Success, string Message, object? Data)>
        InitiatePaymentAsync(int userId, CreateSubscriptionRequest request)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return (false, "Kullanıcı bulunamadı", null);

            // Aktif abonelik kontrolü
            if (await HasActiveSubscriptionAsync(userId))
                return (false, "Zaten aktif aboneliğiniz var", null);

            var settings = await _context.SubscriptionSettings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null)
                return (false, "Abonelik ayarları tanımlı değil", null);

            // Trial kontrolü
            if (settings.TrialEnabled && request.PaymentGateway == PaymentGateway.Iyzico)
            {
                var hasTrial = await _context.Subscriptions
                    .AsNoTracking()
                    .AnyAsync(x => x.UserId == userId && x.IsTrial);

                if (!hasTrial)
                {
                    var trialResult = await ActivateTrialAsync(userId);
                    return (trialResult.Success, trialResult.Message, null);
                }
            }

            // Fiyat hesaplama
            var price = CalculatePrice(request.PlanType, settings);

            return request.PaymentGateway switch
            {
                PaymentGateway.Iyzico => await InitiateIyzicoPaymentAsync(userId, request, price),
                PaymentGateway.GooglePlay => await ProcessGooglePlayPurchaseAsync(userId, request),
                PaymentGateway.AppStore => await ProcessAppStorePurchaseAsync(userId, request),
                _ => (false, "Desteklenmeyen ödeme yöntemi", null)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Payment initiation error for user {UserId}", userId);
            return (false, "Ödeme başlatılırken bir hata oluştu", null);
        }
    }

    private async Task<(bool Success, string Message, object? Data)>
        InitiateIyzicoPaymentAsync(int userId, CreateSubscriptionRequest request, decimal price)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "Kullanıcı bulunamadı", null);

            var productName = request.PlanType == SubscriptionPlanType.Monthly
                ? "Hesapix Aylık Abonelik"
                : "Hesapix Yıllık Abonelik";

            var paymentRequest = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                Price = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                PaidPrice = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                Currency = Currency.TRY.ToString(),
                BasketId = $"B{userId}{DateTime.UtcNow.Ticks}",
                PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString(),
                CallbackUrl = $"{_configuration["AppSettings:BaseUrl"]}/api/subscription/callback",
                EnabledInstallments = new List<int> { 1 }
            };

            // Buyer bilgileri
            paymentRequest.Buyer = new Buyer
            {
                Id = userId.ToString(),
                Name = user.CompanyName ?? "Müşteri",
                Surname = ".",
                Email = user.Email,
                IdentityNumber = "11111111111",
                RegistrationAddress = "Türkiye",
                City = "İstanbul",
                Country = "Turkey",
                Ip = "85.34.78.112"
            };

            // Adres bilgileri
            var address = new Address
            {
                ContactName = user.CompanyName ?? "Müşteri",
                City = "İstanbul",
                Country = "Turkey",
                Description = "Türkiye"
            };

            paymentRequest.ShippingAddress = address;
            paymentRequest.BillingAddress = address;

            // Ürün bilgisi
            paymentRequest.BasketItems = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = "1",
                    Name = productName,
                    Category1 = "Subscription",
                    ItemType = BasketItemType.VIRTUAL.ToString(),
                    Price = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                }
            };

            // Iyzico isteği
            var checkoutForm = CheckoutFormInitialize.Create(paymentRequest, _iyzicoOptions);

            if (checkoutForm.Status != "success")
            {
                Log.Error("Iyzico payment init failed: {Error}", checkoutForm.ErrorMessage);
                return (false, $"Ödeme başlatılamadı: {checkoutForm.ErrorMessage}", null);
            }

            // Pending subscription oluştur
            var pendingSubscription = new Subscription
            {
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.PaymentFailed, // Ödeme bekliyor
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow,
                Price = price,
                FinalPrice = price,
                IsTrial = false,
                AutoRenew = true,
                PaymentGateway = PaymentGateway.Iyzico,
                PaymentToken = checkoutForm.Token,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(pendingSubscription);
            await _context.SaveChangesAsync();

            return (true, "Ödeme sayfasına yönlendiriliyorsunuz", new
            {
                PaymentPageUrl = checkoutForm.PaymentPageUrl,
                Token = checkoutForm.Token,
                Amount = price
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Iyzico payment initialization error");
            return (false, "Ödeme işlemi başlatılamadı", null);
        }
    }

    // ========================================
    // IYZICO CALLBACK
    // ========================================
    public async Task<(bool Success, string Message)> HandleIyzicoCallbackAsync(string token)
    {
        try
        {
            var request = new RetrieveCheckoutFormRequest
            {
                Token = token
            };

            var checkoutForm = CheckoutForm.Retrieve(request, _iyzicoOptions);

            if (checkoutForm.Status != "success" || checkoutForm.PaymentStatus != "SUCCESS")
            {
                Log.Warning("Iyzico payment failed: {Status}", checkoutForm.PaymentStatus);
                return (false, "Ödeme başarısız oldu");
            }

            // Pending subscription'ı bul
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.PaymentToken == token);

            if (subscription == null)
            {
                Log.Error("Subscription not found for token: {Token}", token);
                return (false, "Abonelik bulunamadı");
            }

            // Subscription'ı aktif et
            subscription.Status = SubscriptionStatus.Active;
            subscription.StartDate = DateTime.UtcNow;
            subscription.EndDate = subscription.PlanType == SubscriptionPlanType.Monthly
                ? DateTime.UtcNow.AddMonths(1)
                : DateTime.UtcNow.AddYears(1);
            subscription.PaymentTransactionId = checkoutForm.PaymentId;
            subscription.PaymentDate = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Email gönder
            try
            {
                var user = await _context.Users.FindAsync(subscription.UserId);
                if (user != null)
                {
                    await _emailService.SendSubscriptionActivatedEmailAsync(user.Email, subscription);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send subscription email");
            }

            Log.Information("Subscription activated successfully: {SubscriptionId}", subscription.Id);
            return (true, "Abonelik başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Iyzico callback handling error");
            return (false, "Ödeme doğrulama hatası");
        }
    }

    // ========================================
    // GOOGLE PLAY & APP STORE
    // ========================================
    private async Task<(bool Success, string Message, object? Data)>
        ProcessGooglePlayPurchaseAsync(int userId, CreateSubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.GooglePlayPurchaseToken) ||
                string.IsNullOrEmpty(request.GooglePlayProductId))
            {
                return (false, "Google Play satın alma bilgileri eksik", null);
            }

            // Google Play'den satın alma doğrulama
            var (isValid, transactionId, amount) = await _mobilePaymentService
                .ValidateGooglePlayPurchaseAsync(
                    request.GooglePlayPurchaseToken,
                    request.GooglePlayProductId
                );

            if (!isValid)
            {
                return (false, "Google Play satın alma doğrulanamadı", null);
            }

            // Subscription oluştur
            var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();
            var subscription = new Subscription
            {
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = request.PlanType == SubscriptionPlanType.Monthly
                    ? DateTime.UtcNow.AddMonths(1)
                    : DateTime.UtcNow.AddYears(1),
                Price = amount,
                FinalPrice = amount,
                IsTrial = false,
                AutoRenew = true,
                PaymentGateway = PaymentGateway.GooglePlay,
                PaymentTransactionId = transactionId,
                PaymentToken = request.GooglePlayPurchaseToken,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            // Acknowledge the purchase
            await _mobilePaymentService.AcknowledgeGooglePlayPurchaseAsync(
                request.GooglePlayPurchaseToken
            );

            Log.Information("Google Play subscription created: {SubscriptionId}", subscription.Id);
            return (true, "Abonelik başarıyla oluşturuldu", null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play subscription error");
            return (false, "Google Play abonelik işlemi başarısız", null);
        }
    }

    private async Task<(bool Success, string Message, object? Data)>
        ProcessAppStorePurchaseAsync(int userId, CreateSubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AppStoreReceiptData) ||
                string.IsNullOrEmpty(request.AppStoreTransactionId))
            {
                return (false, "App Store satın alma bilgileri eksik", null);
            }

            // App Store'dan satın alma doğrulama
            var (isValid, transactionId, amount) = await _mobilePaymentService
                .ValidateAppStorePurchaseAsync(
                    request.AppStoreReceiptData,
                    request.AppStoreTransactionId
                );

            if (!isValid)
            {
                return (false, "App Store satın alma doğrulanamadı", null);
            }

            // Subscription oluştur
            var subscription = new Subscription
            {
                UserId = userId,
                PlanType = request.PlanType,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = request.PlanType == SubscriptionPlanType.Monthly
                    ? DateTime.UtcNow.AddMonths(1)
                    : DateTime.UtcNow.AddYears(1),
                Price = amount,
                FinalPrice = amount,
                IsTrial = false,
                AutoRenew = true,
                PaymentGateway = PaymentGateway.AppStore,
                PaymentTransactionId = transactionId,
                PaymentToken = request.AppStoreReceiptData,
                PaymentDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            Log.Information("App Store subscription created: {SubscriptionId}", subscription.Id);
            return (true, "Abonelik başarıyla oluşturuldu", null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "App Store subscription error");
            return (false, "App Store abonelik işlemi başarısız", null);
        }
    }

    // ========================================
    // TRIAL
    // ========================================
    public async Task<(bool Success, string Message)> ActivateTrialAsync(int userId)
    {
        try
        {
            var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();
            if (settings == null || !settings.TrialEnabled)
                return (false, "Deneme sürümü aktif değil");

            var hasUsedTrial = await _context.Subscriptions
                .AsNoTracking()
                .AnyAsync(x => x.UserId == userId && x.IsTrial);

            if (hasUsedTrial)
                return (false, "Deneme sürümü daha önce kullanılmış");

            var subscription = new Subscription
            {
                UserId = userId,
                PlanType = SubscriptionPlanType.Monthly,
                Status = SubscriptionStatus.Trial,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(settings.TrialDurationDays),
                Price = 0,
                FinalPrice = 0,
                IsTrial = true,
                AutoRenew = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            Log.Information("Trial activated for user {UserId}", userId);
            return (true, $"Deneme sürümü başlatıldı ({settings.TrialDurationDays} gün)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Trial activation error for user {UserId}", userId);
            return (false, "Deneme sürümü başlatılamadı");
        }
    }

    // ========================================
    // READ OPERATIONS
    // ========================================
    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId)
    {
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
                x.EndDate > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        return subscription == null ? null : _mapper.Map<SubscriptionDto>(subscription);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId &&
                (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
                x.EndDate > DateTime.UtcNow);
    }

    // ========================================
    // CANCEL / REACTIVATE
    // ========================================
    public async Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Where(x => x.UserId == userId &&
                    (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
                    !x.WillCancelAtPeriodEnd)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return (false, "İptal edilebilecek aktif abonelik bulunamadı");

            subscription.WillCancelAtPeriodEnd = true;
            subscription.AutoRenew = false;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("Subscription cancelled for user {UserId}", userId);
            return (true, "Aboneliğiniz dönem sonunda iptal edilecek");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Subscription cancellation error");
            return (false, "Abonelik iptal edilemedi");
        }
    }

    public async Task<(bool Success, string Message)> ReactivateSubscriptionAsync(int userId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .Where(x => x.UserId == userId &&
                    x.WillCancelAtPeriodEnd &&
                    x.EndDate > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return (false, "Yeniden aktif edilebilecek abonelik bulunamadı");

            subscription.WillCancelAtPeriodEnd = false;
            subscription.AutoRenew = true;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            Log.Information("Subscription reactivated for user {UserId}", userId);
            return (true, "Aboneliğiniz yeniden aktif edildi");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Subscription reactivation error");
            return (false, "Abonelik aktif edilemedi");
        }
    }

    // ========================================
    // PLANS
    // ========================================
    public async Task<object> GetAvailablePlansAsync()
    {
        var settings = await _context.SubscriptionSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null)
            return new { Plans = new List<object>() };

        var plans = new List<object>();

        if (settings.TrialEnabled)
        {
            plans.Add(new
            {
                Type = "Trial",
                Name = "Deneme Sürümü",
                Price = 0m,
                DurationDays = settings.TrialDurationDays,
                Description = $"{settings.TrialDurationDays} gün ücretsiz deneme"
            });
        }

        plans.Add(new
        {
            Type = "Monthly",
            Name = "Aylık Plan",
            Price = settings.MonthlyPrice,
            Duration = "1 Ay",
            Description = "Aylık abonelik planı"
        });

        plans.Add(new
        {
            Type = "Yearly",
            Name = "Yıllık Plan",
            Price = settings.YearlyPrice,
            Duration = "1 Yıl",
            Description = "Yıllık abonelik planı - %17 indirim",
            SaveAmount = (settings.MonthlyPrice * 12) - settings.YearlyPrice
        });

        return new
        {
            Plans = plans,
            Campaign = settings.CampaignEnabled ? new
            {
                Enabled = true,
                DiscountPercent = settings.CampaignDiscountPercent,
                Description = $"%{settings.CampaignDiscountPercent} kampanya indirimi"
            } : null
        };
    }

    // ========================================
    // EXPIRED SUBSCRIPTIONS
    // ========================================
    public async Task ProcessExpiredSubscriptionsAsync()
    {
        try
        {
            var expiredSubscriptions = await _context.Subscriptions
                .Where(x => x.EndDate <= DateTime.UtcNow &&
                    (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial))
                .ToListAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = subscription.WillCancelAtPeriodEnd
                    ? SubscriptionStatus.Cancelled
                    : SubscriptionStatus.Expired;
                subscription.UpdatedAt = DateTime.UtcNow;

                // Email bildirimi
                try
                {
                    var user = await _context.Users.FindAsync(subscription.UserId);
                    if (user != null && subscription.Status == SubscriptionStatus.Expired)
                    {
                        await _emailService.SendSubscriptionExpiredEmailAsync(user.Email);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to send expiration email");
                }
            }

            if (expiredSubscriptions.Any())
            {
                await _context.SaveChangesAsync();
                Log.Information("Processed {Count} expired subscriptions", expiredSubscriptions.Count);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing expired subscriptions");
        }
    }

    // ========================================
    // WEBHOOK
    // ========================================
    public async Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload)
    {
        try
        {
            Log.Information("Iyzico webhook received: {Payload}", payload);
            // TODO: Webhook validation ve işleme
            return (true, "Webhook işlendi");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Iyzico webhook error");
            return (false, "Webhook işlenemedi");
        }
    }

    // ========================================
    // PRIVATE HELPERS
    // ========================================
    private decimal CalculatePrice(SubscriptionPlanType planType, SubscriptionSettings settings)
    {
        var basePrice = planType == SubscriptionPlanType.Monthly
            ? settings.MonthlyPrice
            : settings.YearlyPrice;

        if (settings.CampaignEnabled)
        {
            var discount = basePrice * (settings.CampaignDiscountPercent / 100m);
            return basePrice - discount;
        }

        return basePrice;
    }
}