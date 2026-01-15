namespace Hesapix.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
    Task<bool> SendVerificationEmailAsync(string toEmail, string verificationLink);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink);
    Task<bool> SendSubscriptionConfirmationEmailAsync(string toEmail, string companyName, DateTime endDate);
    Task<bool> SendSubscriptionExpirationWarningAsync(string toEmail, string companyName, int daysRemaining);
}