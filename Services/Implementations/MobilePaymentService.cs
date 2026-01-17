using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Hesapix.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Hesapix.Services.Implementations;

public class MobilePaymentService : IMobilePaymentService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly int _maxRetries = 3;

    public MobilePaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    // ========================================
    // GOOGLE PLAY
    // ========================================
    public async Task<(bool IsValid, string? TransactionId, decimal Amount)>
        ValidateGooglePlayPurchaseAsync(string purchaseToken, string productId)
    {
        try
        {
            var packageName = _configuration["GooglePlay:PackageName"];
            var serviceAccountKeyPath = _configuration["GooglePlay:ServiceAccountKeyPath"];

            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(serviceAccountKeyPath))
            {
                Log.Warning("Google Play configuration is missing");
                return (false, null, 0);
            }

            // Service account ile authenticate
            var credential = await GoogleCredential
                .FromFileAsync(serviceAccountKeyPath, CancellationToken.None);

            var scopedCredential = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            // Android Publisher API servis oluştur
            var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = scopedCredential,
                ApplicationName = "Hesapix"
            });

            // Subscription purchase bilgisini al
            var request = service.Purchases.Subscriptionsv2.Get(
                packageName,
                purchaseToken
            );

            var purchase = await request.ExecuteAsync();

            // Ödeme durumunu kontrol et
            if (purchase.SubscriptionState != "SUBSCRIPTION_STATE_ACTIVE")
            {
                Log.Warning("Google Play subscription is not active: {State}", purchase.SubscriptionState);
                return (false, null, 0);
            }

            // Transaction ID ve amount hesapla
            var orderId = purchase.LatestOrderId;
            var amount = CalculateAmountFromProductId(productId);

            Log.Information("Google Play purchase validated successfully: {OrderId}", orderId);
            return (true, orderId, amount);
        }
        catch (Google.GoogleApiException ex)
        {
            Log.Error(ex, "Google Play API error: {StatusCode} - {Message}",
                ex.HttpStatusCode, ex.Message);
            return (false, null, 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play validation error");
            return (false, null, 0);
        }
    }

    public async Task<bool> AcknowledgeGooglePlayPurchaseAsync(string purchaseToken)
    {
        try
        {
            var packageName = _configuration["GooglePlay:PackageName"];
            var serviceAccountKeyPath = _configuration["GooglePlay:ServiceAccountKeyPath"];
            var productId = _configuration["GooglePlay:MonthlyProductId"]; // veya YearlyProductId

            var credential = await GoogleCredential
                .FromFileAsync(serviceAccountKeyPath, CancellationToken.None);

            var scopedCredential = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = scopedCredential,
                ApplicationName = "Hesapix"
            });

            // Acknowledge request body oluştur
            var acknowledgeRequest = new SubscriptionPurchasesAcknowledgeRequest
            {
                DeveloperPayload = "Hesapix subscription"
            };

            // Acknowledge isteği gönder
            await service.Purchases.Subscriptions.Acknowledge(
                acknowledgeRequest,
                packageName,
                productId,
                purchaseToken
            ).ExecuteAsync();

            Log.Information("Google Play purchase acknowledged: {Token}", purchaseToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play acknowledge failed");
            return false;
        }
    }

    public async Task<bool> RefundGooglePlayPurchaseAsync(string purchaseToken)
    {
        try
        {
            var packageName = _configuration["GooglePlay:PackageName"];
            var serviceAccountKeyPath = _configuration["GooglePlay:ServiceAccountKeyPath"];
            var productId = _configuration["GooglePlay:MonthlyProductId"];

            var credential = await GoogleCredential
                .FromFileAsync(serviceAccountKeyPath, CancellationToken.None);

            var scopedCredential = credential.CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

            var service = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = scopedCredential,
                ApplicationName = "Hesapix"
            });

            // Refund request
            await service.Purchases.Subscriptions.Refund(
                packageName,
                productId,
                purchaseToken
            ).ExecuteAsync();

            Log.Information("Google Play purchase refunded: {Token}", purchaseToken);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play refund failed");
            return false;
        }
    }

    // ========================================
    // APP STORE
    // ========================================
    public async Task<(bool IsValid, string? TransactionId, decimal Amount)>
        ValidateAppStorePurchaseAsync(string receiptData, string transactionId)
    {
        try
        {
            var sharedSecret = _configuration["AppStore:SharedSecret"];
            var useSandbox = _configuration.GetValue<bool>("AppStore:UseSandbox");

            if (string.IsNullOrEmpty(sharedSecret))
            {
                Log.Warning("App Store configuration is missing");
                return (false, null, 0);
            }

            // İlk olarak production'da dene
            var (isValid, receipt) = await VerifyAppStoreReceiptAsync(
                receiptData,
                sharedSecret,
                false
            );

            // Production başarısız olursa ve sandbox aktifse sandbox'da dene
            if (!isValid && useSandbox)
            {
                (isValid, receipt) = await VerifyAppStoreReceiptAsync(
                    receiptData,
                    sharedSecret,
                    true
                );
            }

            if (!isValid || receipt == null)
            {
                return (false, null, 0);
            }

            // Transaction'ı bul
            var transaction = receipt.latest_receipt_info?
                .FirstOrDefault(t => t.transaction_id == transactionId);

            if (transaction == null)
            {
                Log.Warning("App Store transaction not found: {TransactionId}", transactionId);
                return (false, null, 0);
            }

            // Subscription'ın aktif olduğunu doğrula
            if (!string.IsNullOrEmpty(transaction.expires_date_ms))
            {
                var expiresDate = DateTimeOffset
                    .FromUnixTimeMilliseconds(long.Parse(transaction.expires_date_ms))
                    .DateTime;

                if (expiresDate < DateTime.UtcNow)
                {
                    Log.Warning("App Store subscription expired: {TransactionId}", transactionId);
                    return (false, null, 0);
                }
            }

            var amount = CalculateAmountFromProductId(transaction.product_id);

            Log.Information("App Store purchase validated: {TransactionId}", transactionId);
            return (true, transactionId, amount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "App Store validation error");
            return (false, null, 0);
        }
    }

    private async Task<(bool IsValid, AppStoreReceipt? Receipt)>
        VerifyAppStoreReceiptAsync(string receiptData, string sharedSecret, bool useSandbox)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                var url = useSandbox
                    ? "https://sandbox.itunes.apple.com/verifyReceipt"
                    : "https://buy.itunes.apple.com/verifyReceipt";

                var requestBody = new
                {
                    receipt_data = receiptData,
                    password = sharedSecret,
                    exclude_old_transactions = true
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, httpContent);
                var content = await response.Content.ReadAsStringAsync();
                var receipt = JsonSerializer.Deserialize<AppStoreReceipt>(content);

                if (receipt == null)
                {
                    return (false, null);
                }

                // Status code kontrolü
                if (receipt.status == 0)
                {
                    return (true, receipt);
                }
                else if (receipt.status == 21007 && !useSandbox)
                {
                    // Production'a sandbox receipt gönderilmiş, sandbox'da tekrar dene
                    return await VerifyAppStoreReceiptAsync(receiptData, sharedSecret, true);
                }
                else if (receipt.status == 21008 && useSandbox)
                {
                    // Sandbox'a production receipt gönderilmiş, production'da tekrar dene
                    return await VerifyAppStoreReceiptAsync(receiptData, sharedSecret, false);
                }

                Log.Warning("App Store validation failed: Status {Status}", receipt.status);
                return (false, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "App Store verification attempt {Attempt} failed", attempt + 1);
                if (attempt < _maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                }
            }
        }

        return (false, null);
    }

    // ========================================
    // HELPER METHODS
    // ========================================
    private decimal CalculateAmountFromProductId(string productId)
    {
        var monthlyProductIds = new[]
        {
            _configuration["GooglePlay:MonthlyProductId"],
            _configuration["AppStore:MonthlyProductId"],
            "hesapix_monthly"
        };

        var yearlyProductIds = new[]
        {
            _configuration["GooglePlay:YearlyProductId"],
            _configuration["AppStore:YearlyProductId"],
            "hesapix_yearly"
        };

        if (monthlyProductIds.Contains(productId))
            return 299.00m;

        if (yearlyProductIds.Contains(productId))
            return 2990.00m;

        Log.Warning("Unknown product ID: {ProductId}", productId);
        return 0m;
    }

    // ========================================
    // HELPER CLASSES
    // ========================================
    private class AppStoreReceipt
    {
        public int status { get; set; }
        public string? environment { get; set; }
        public List<AppStoreTransaction>? latest_receipt_info { get; set; }
        public AppStorePendingRenewalInfo? pending_renewal_info { get; set; }
    }

    private class AppStoreTransaction
    {
        public string transaction_id { get; set; } = string.Empty;
        public string original_transaction_id { get; set; } = string.Empty;
        public string product_id { get; set; } = string.Empty;
        public string purchase_date_ms { get; set; } = string.Empty;
        public string expires_date_ms { get; set; } = string.Empty;
        public string? cancellation_date_ms { get; set; }
        public string? is_trial_period { get; set; }
    }

    private class AppStorePendingRenewalInfo
    {
        public string auto_renew_status { get; set; } = string.Empty;
        public string? auto_renew_product_id { get; set; }
    }
}