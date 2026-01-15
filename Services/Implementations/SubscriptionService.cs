using AutoMapper;
using Hesapix.Controllers;
using Hesapix.Data;
using Hesapix.Models.Common;
using Hesapix.Models.DTOs.Subs;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<SubscriptionService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<SubscriptionDTO>> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            // Kullanıcı kontrolü
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<SubscriptionDTO>.FailResult("Kullanıcı bulunamadı");
            }

            // Mevcut aktif abonelik kontrolü
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.Status == "Active");

            if (existingSubscription != null)
            {
                return ApiResponse<SubscriptionDTO>.FailResult("Kullanıcının zaten aktif bir aboneliği var");
            }

            var subscription = new Subscription
            {
                UserId = request.UserId,
                PlanType = request.PlanType,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = request.EndDate,
                Status = "Active",
                Price = request.Price,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            var subscriptionDto = _mapper.Map<SubscriptionDTO>(subscription);
            return ApiResponse<SubscriptionDTO>.SuccessResult(subscriptionDto, "Abonelik oluşturuldu");
        }

        public async Task<ApiResponse<SubscriptionDTO>> UpdateSubscriptionAsync(int subscriptionId, UpdateSubscriptionRequest request)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
            {
                return ApiResponse<SubscriptionDTO>.FailResult("Abonelik bulunamadı");
            }

            if (!string.IsNullOrEmpty(request.PlanType))
            {
                subscription.PlanType = request.PlanType;
            }

            if (request.EndDate.HasValue)
            {
                subscription.EndDate = request.EndDate.Value;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                subscription.Status = request.Status;
            }

            if (request.Price.HasValue)
            {
                subscription.Price = request.Price.Value;
            }

            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var subscriptionDto = _mapper.Map<SubscriptionDTO>(subscription);
            return ApiResponse<SubscriptionDTO>.SuccessResult(subscriptionDto, "Abonelik güncellendi");
        }

        public async Task<ApiResponse<bool>> DeleteSubscriptionAsync(int subscriptionId)
        {
            var subscription = await _context.Subscriptions.FindAsync(subscriptionId);

            if (subscription == null)
            {
                return ApiResponse<bool>.FailResult("Abonelik bulunamadı");
            }

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(true, "Abonelik silindi");
        }

        public async Task<ApiResponse<List<SubscriptionDTO>>> GetAllSubscriptionsAsync(int page, int pageSize, string? status)
        {
            var query = _context.Subscriptions
                .Include(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            var totalCount = await query.CountAsync();

            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var subscriptionDtos = _mapper.Map<List<SubscriptionDTO>>(subscriptions);

            return ApiResponse<List<SubscriptionDTO>>.SuccessResult(subscriptionDtos, $"Toplam {totalCount} abonelik bulundu");
        }

        public async Task<ApiResponse<SubscriptionDTO>> GetSubscriptionByIdAsync(int subscriptionId)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription == null)
            {
                return ApiResponse<SubscriptionDTO>.FailResult("Abonelik bulunamadı");
            }

            var subscriptionDto = _mapper.Map<SubscriptionDTO>(subscription);
            return ApiResponse<SubscriptionDTO>.SuccessResult(subscriptionDto);
        }

        public async Task<ApiResponse<SubscriptionDTO>> GetUserSubscriptionAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
            {
                return ApiResponse<SubscriptionDTO>.FailResult("Abonelik bulunamadı");
            }

            var subscriptionDto = _mapper.Map<SubscriptionDTO>(subscription);
            return ApiResponse<SubscriptionDTO>.SuccessResult(subscriptionDto);
        }

        public async Task<ApiResponse<AdminStatisticsDto>> GetAdminStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Active");
            var expiredSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Expired");
            var cancelledSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Cancelled");
            var totalRevenue = await _context.Subscriptions
                .Where(s => s.Status == "Active")
                .SumAsync(s => s.Price);

            var subscriptionsByPlan = await _context.Subscriptions
                .Where(s => s.Status == "Active")
                .GroupBy(s => s.PlanType)
                .Select(g => new { PlanType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PlanType, x => x.Count);

            var statistics = new AdminStatisticsDto
            {
                TotalUsers = totalUsers,
                ActiveSubscriptions = activeSubscriptions,
                ExpiredSubscriptions = expiredSubscriptions,
                CancelledSubscriptions = cancelledSubscriptions,
                TotalRevenue = totalRevenue,
                SubscriptionsByPlan = subscriptionsByPlan
            };

            return ApiResponse<AdminStatisticsDto>.SuccessResult(statistics);
        }

        public async Task<ApiResponse<bool>> CheckAndUpdateExpiredSubscriptionsAsync()
        {
            var expiredSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == "Active" &&
                           s.EndDate != null &&
                           s.EndDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var subscription in expiredSubscriptions)
            {
                subscription.Status = "Expired";
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            if (expiredSubscriptions.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Count} abonelik süresi doldu ve güncellendi", expiredSubscriptions.Count);
            }

            return ApiResponse<bool>.SuccessResult(true, $"{expiredSubscriptions.Count} abonelik güncellendi");
        }
    }
}