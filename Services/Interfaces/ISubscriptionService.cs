
using Hesapix.Controllers;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Subs;
using Hesapix.Models.DTOs.Subscription;

namespace Hesapix.Services.Interfaces
{
    public interface ISubscriptionService
    {
        Task<ApiResponse<SubscriptionDTO>> CreateSubscriptionAsync(CreateSubscriptionRequest request);
        Task<ApiResponse<SubscriptionDTO>> UpdateSubscriptionAsync(int subscriptionId, UpdateSubscriptionRequest request);
        Task<ApiResponse<bool>> DeleteSubscriptionAsync(int subscriptionId);
        Task<ApiResponse<List<SubscriptionDTO>>> GetAllSubscriptionsAsync(int page, int pageSize, string? status);
        Task<ApiResponse<SubscriptionDTO>> GetSubscriptionByIdAsync(int subscriptionId);
        Task<ApiResponse<SubscriptionDTO>> GetUserSubscriptionAsync(int userId);
        Task<ApiResponse<AdminStatisticsDto>> GetAdminStatisticsAsync();
        Task<ApiResponse<bool>> CheckAndUpdateExpiredSubscriptionsAsync();
    }
}