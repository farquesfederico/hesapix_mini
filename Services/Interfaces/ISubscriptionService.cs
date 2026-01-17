using Hesapix.Models.DTOs.Subscription;

namespace Hesapix.Services.Interfaces;

public interface ISubscriptionService
{
    // Payment Operations
    Task<(bool Success, string Message, object? Data)> InitiatePaymentAsync(
        int userId,
        CreateSubscriptionRequest request);

    Task<(bool Success, string Message)> HandleIyzicoCallbackAsync(string token);
    Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload);

    // Trial Operations
    Task<(bool Success, string Message)> ActivateTrialAsync(int userId);

    // Read Operations
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId);
    Task<bool> HasActiveSubscriptionAsync(int userId);
    Task<object> GetAvailablePlansAsync();

    // Lifecycle Operations
    Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId);
    Task<(bool Success, string Message)> ReactivateSubscriptionAsync(int userId);
    Task ProcessExpiredSubscriptionsAsync();
}