using Hesapix.Models.Entities;

namespace Hesapix.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
    Task<bool> SendVerificationEmailAsync(string to, string verificationToken);
    Task<bool> SendPasswordResetEmailAsync(string to, string resetToken);
    Task<bool> SendSubscriptionActivatedEmailAsync(string to, Subscription subscription);
    Task<bool> SendSubscriptionExpiredEmailAsync(string to);
    Task<bool> SendSubscriptionCancelledEmailAsync(string to);
    Task<bool> SendSubscriptionRenewalReminderAsync(string to, Subscription subscription);
}