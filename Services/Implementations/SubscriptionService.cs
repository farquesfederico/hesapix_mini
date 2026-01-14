using Hesapix.Data;
using Hesapix.Models.Entities;
using Hesapix.Models.DTOs.Subscription;
using Hesapix.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hesapix.Services.Implementations
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin tarafından abonelik oluşturma
        public async Task<SubscriptionDto> CreateSubscription(CreateSubscriptionRequest request, int adminId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null) throw new Exception("Kullanıcı bulunamadı");

            var subscription = new Subscription
            {
                UserId = request.UserId,
                Type = request.Type,
                Platform = request.Platform,
                StartDate = DateTime.UtcNow,
                EndDate = request.Type == SubscriptionType.Monthly ? DateTime.UtcNow.AddMonths(1) :
                          request.Type == SubscriptionType.Yearly ? DateTime.UtcNow.AddYears(1) :
                          DateTime.MaxValue,
                Amount = request.Amount,
                IsActive = true,
                TransactionId = request.TransactionId,
                ReceiptData = request.ReceiptData
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return MapToDto(subscription);
        }

        // Kullanıcının aboneliğini kontrol et
        public async Task<bool> CheckSubscription(int userId)
        {
            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .FirstOrDefaultAsync();

            if (subscription == null) return false;

            if (subscription.EndDate < DateTime.UtcNow)
            {
                subscription.IsActive = false;
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }

        // Kullanıcı aboneliklerini getir
        public async Task<List<SubscriptionDto>> GetSubscriptions(int userId)
        {
            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            return subscriptions.Select(MapToDto).ToList();
        }

        // ID ile abonelik getir
        public async Task<SubscriptionDto> GetSubscriptionById(int id, int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (subscription == null) throw new Exception("Abonelik bulunamadı");

            return MapToDto(subscription);
        }

        // Aboneliği aktif et
        public async Task<bool> ActivateSubscription(int subscriptionId, int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

            if (subscription == null) return false;

            subscription.IsActive = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Aboneliği pasif yap
        public async Task<bool> DeactivateSubscription(int subscriptionId, int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

            if (subscription == null) return false;

            subscription.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // Admin tüm abonelikleri listeleyebilir
        public async Task<List<SubscriptionDto>> GetAllSubscriptions()
        {
            var subscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .ToListAsync();

            return subscriptions.Select(MapToDto).ToList();
        }

        private SubscriptionDto MapToDto(Subscription s)
        {
            return new SubscriptionDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserEmail = s.User?.Email,
                Type = s.Type,
                Platform = s.Platform,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                Amount = s.Amount,
                TransactionId = s.TransactionId
            };
        }
    }
}
