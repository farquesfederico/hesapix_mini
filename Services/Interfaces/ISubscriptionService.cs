using Hesapix.Models.DTOs.Subscription;

namespace Hesapix.Services.Interfaces;

public interface ISubscriptionService
{
    Task<(bool Success, string Message, object? Data)> CreateSubscriptionAsync(int userId, CreateSubscriptionRequest request);
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId);
    Task<bool> HasActiveSubscriptionAsync(int userId);
    Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId);
    Task<(bool Success, string Message)> HandleIyzicoWebhookAsync(string payload);
    Task ProcessExpiredSubscriptionsAsync();
    Task<decimal> GetSubscriptionPriceAsync(CreateSubscriptionRequest request);
}