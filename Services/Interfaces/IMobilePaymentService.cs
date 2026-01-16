namespace Hesapix.Services.Interfaces;

public interface IMobilePaymentService
{
    Task<(bool IsValid, string? TransactionId, decimal Amount)> ValidateGooglePlayPurchaseAsync(
        string purchaseToken, string productId);

    Task<(bool IsValid, string? TransactionId, decimal Amount)> ValidateAppStorePurchaseAsync(
        string receiptData, string transactionId);

    Task<bool> AcknowledgeGooglePlayPurchaseAsync(string purchaseToken);
    Task<bool> RefundGooglePlayPurchaseAsync(string purchaseToken);
}