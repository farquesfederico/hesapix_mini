using Hesapix.Models.DTOs.Subscription;

namespace Hesapix.Services.Interfaces
{
    public interface ISubscriptionService
    {
        // Admin işlemleri
        Task<SubscriptionDto> CreateSubscription(CreateSubscriptionRequest request, int adminId);
        Task<SubscriptionDto> GetSubscriptionById(int id, int adminId);
        Task<List<SubscriptionDto>> GetSubscriptions(int adminId); // Admin tüm abonelikleri görebilir
        Task<bool> ActivateSubscription(int id, int adminId);      // Admin aktivasyon
        Task<bool> DeactivateSubscription(int id, int adminId);    // Admin iptal
        Task<List<SubscriptionDto>> GetAllSubscriptions();         // Admin tüm abonelikleri listele

        // Kullanıcı işlemleri
        Task<bool> CheckSubscription(int userId);                 // Kullanıcı abonelik kontrolü
    }
}
