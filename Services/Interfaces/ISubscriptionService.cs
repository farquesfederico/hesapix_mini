using Hesapix.Models.DTOs.Subscription;

namespace Hesapix.Services.Interfaces;

public interface ISubscriptionService
{
    Task<(bool Success, string Message, object? Data)> InitiatePaymentAsync(int userId, CreateSubscriptionRequest request);
    Task<(bool Success, string Message)> CompletePaymentAsync(int userId, CreateSubscriptionRequest request, string transactionId);
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId);
    Task<bool> HasActiveSubscriptionAsync(int userId);
    Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId);
    Task<(bool Success, string Message)> ReactivateSubscriptionAsync(int userId); // ← YENİ!
    Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload);
    Task ProcessExpiredSubscriptionsAsync();
    Task<(bool Success, string Message)> ActivateTrialAsync(int userId);
    Task<object> GetAvailablePlansAsync();
}