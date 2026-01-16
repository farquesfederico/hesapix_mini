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

    public MobilePaymentService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<(bool IsValid, string? TransactionId, decimal Amount)> ValidateGooglePlayPurchaseAsync(
        string purchaseToken, string productId)
    {
        try
        {
            // Google Play Developer API kullanarak doğrulama
            var packageName = _configuration["GooglePlay:PackageName"];
            var serviceAccountJson = _configuration["GooglePlay:ServiceAccountJson"];

            // TODO: Google.Apis.AndroidPublisher.v3 paketi ile implement edilmeli
            // https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.subscriptions/get

            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/subscriptions/{productId}/tokens/{purchaseToken}";

            // OAuth 2.0 token al
            var accessToken = await GetGoogleAccessTokenAsync(serviceAccountJson);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Google Play validation failed: {StatusCode}", response.StatusCode);
                return (false, null, 0);
            }

            var content = await response.Content.ReadAsStringAsync();
            var purchase = JsonSerializer.Deserialize<GooglePlayPurchase>(content);

            if (purchase == null || purchase.paymentState != 1) // 1 = Paid
            {
                return (false, null, 0);
            }

            // Fiyatı hesapla
            var amount = CalculateAmountFromProductId(productId);

            Log.Information("Google Play purchase validated: {Token}", purchaseToken);
            return (true, purchase.orderId, amount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play validation error");
            return (false, null, 0);
        }
    }

    public async Task<(bool IsValid, string? TransactionId, decimal Amount)> ValidateAppStorePurchaseAsync(
        string receiptData, string transactionId)
    {
        try
        {
            // Apple App Store Server API ile doğrulama
            // https://developer.apple.com/documentation/appstorereceipts/verifyreceipt

            var sharedSecret = _configuration["AppStore:SharedSecret"];
            var useSandbox = _configuration.GetValue<bool>("AppStore:UseSandbox");

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

            if (receipt == null || receipt.status != 0) // 0 = Valid
            {
                Log.Warning("App Store validation failed: Status {Status}", receipt?.status);
                return (false, null, 0);
            }

            // Transaction'ı bul
            var transaction = receipt.latest_receipt_info?.FirstOrDefault(t => t.transaction_id == transactionId);

            if (transaction == null)
            {
                return (false, null, 0);
            }

            // Fiyatı hesapla
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

    public async Task<bool> AcknowledgeGooglePlayPurchaseAsync(string purchaseToken)
    {
        try
        {
            // Google Play'de satın alma onayı
            var packageName = _configuration["GooglePlay:PackageName"];
            var serviceAccountJson = _configuration["GooglePlay:ServiceAccountJson"];
            var accessToken = await GetGoogleAccessTokenAsync(serviceAccountJson);

            var url = $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{packageName}/purchases/products/tokens/{purchaseToken}:acknowledge";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
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
            // İade işlemi
            // TODO: Implement refund logic
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Google Play refund failed");
            return false;
        }
    }

    private async Task<string> GetGoogleAccessTokenAsync(string serviceAccountJson)
    {
        // TODO: Google OAuth 2.0 ile access token al
        // Google.Apis.Auth paketi kullanılmalı
        await Task.CompletedTask;
        return "mock-access-token";
    }

    private decimal CalculateAmountFromProductId(string productId)
    {
        // Product ID'den fiyat hesapla
        return productId switch
        {
            "hesapix_monthly" => 299.00m,
            "hesapix_yearly" => 2990.00m,
            _ => 0m
        };
    }

    // Helper classes
    private class GooglePlayPurchase
    {
        public int paymentState { get; set; }
        public string orderId { get; set; } = string.Empty;
        public long expiryTimeMillis { get; set; }
    }

    private class AppStoreReceipt
    {
        public int status { get; set; }
        public List<AppStoreTransaction>? latest_receipt_info { get; set; }
    }

    private class AppStoreTransaction
    {
        public string transaction_id { get; set; } = string.Empty;
        public string product_id { get; set; } = string.Empty;
        public string expires_date { get; set; } = string.Empty;
    }
}