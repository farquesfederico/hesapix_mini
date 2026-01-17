using Hesapix.Models.Entities;
using Hesapix.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Serilog;

namespace Hesapix.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = emailSettings.GetValue<int>("SmtpPort");
            var smtpUsername = emailSettings["SmtpUsername"];
            var smtpPassword = emailSettings["SmtpPassword"];
            var fromEmail = emailSettings["FromEmail"];
            var fromName = emailSettings["FromName"];

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
            {
                Log.Warning("Email configuration is missing. Email not sent.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Log.Information("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string verificationToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";

        var verificationLink = $"{baseUrl}/verify-email?token={verificationToken}";

        var subject = "Hesapix - Email Doğrulama";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 24px; 
                        margin: 20px 0; 
                        background-color: #007bff; 
                        color: white !important; 
                        text-decoration: none; 
                        border-radius: 4px; 
                    }}
                    .footer {{ color: #666; font-size: 14px; margin-top: 30px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Hoş Geldiniz!</h2>
                    <p>Hesapix'e kayıt olduğunuz için teşekkür ederiz.</p>
                    <p>Lütfen aşağıdaki bağlantıya tıklayarak email adresinizi doğrulayın:</p>
                    <a href='{verificationLink}' class='button'>Email Adresimi Doğrula</a>
                    <p class='footer'>
                        Bu link 24 saat geçerlidir. Eğer bu işlemi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.
                    </p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://hesapix.com";
        var resetLink = $"{baseUrl}/reset-password?token={resetToken}";

        var subject = "Hesapix - Şifre Sıfırlama";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 24px; 
                        margin: 20px 0; 
                        background-color: #dc3545; 
                        color: white !important; 
                        text-decoration: none; 
                        border-radius: 4px; 
                    }}
                    .footer {{ color: #666; font-size: 14px; margin-top: 30px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Şifre Sıfırlama Talebi</h2>
                    <p>Şifrenizi sıfırlamak için bir talepte bulundunuz.</p>
                    <p>Lütfen aşağıdaki bağlantıya tıklayarak yeni şifrenizi belirleyin:</p>
                    <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
                    <p class='footer'>
                        Bu link 1 saat geçerlidir. Eğer bu işlemi siz yapmadıysanız, lütfen bu emaili görmezden geçin.
                    </p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionActivatedEmailAsync(string toEmail, Subscription subscription)
    {
        var subject = "Hesapix - Aboneliğiniz Aktif!";
        var planName = subscription.PlanType == Models.Enums.SubscriptionPlanType.Monthly
            ? "Aylık"
            : "Yıllık";

        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .success {{ color: #28a745; }}
                    .info-box {{ 
                        background-color: #f8f9fa; 
                        padding: 15px; 
                        border-left: 4px solid #28a745; 
                        margin: 20px 0; 
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2 class='success'>🎉 Aboneliğiniz Aktif!</h2>
                    <p>Hesapix {planName} aboneliğiniz başarıyla aktif edilmiştir.</p>
                    <div class='info-box'>
                        <p><strong>Plan:</strong> {planName}</p>
                        <p><strong>Başlangıç:</strong> {subscription.StartDate:dd.MM.yyyy}</p>
                        <p><strong>Bitiş:</strong> {subscription.EndDate:dd.MM.yyyy}</p>
                        <p><strong>Ödenen Tutar:</strong> {subscription.FinalPrice:F2} TL</p>
                    </div>
                    <p>Artık tüm premium özelliklerimizden yararlanabilirsiniz!</p>
                    <p style='margin-top: 30px;'>İyi çalışmalar dileriz,<br/>Hesapix Ekibi</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionExpiredEmailAsync(string toEmail)
    {
        var subject = "Hesapix - Aboneliğiniz Sona Erdi";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .warning {{ color: #ffc107; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 24px; 
                        margin: 20px 0; 
                        background-color: #007bff; 
                        color: white !important; 
                        text-decoration: none; 
                        border-radius: 4px; 
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2 class='warning'>⚠️ Aboneliğiniz Sona Erdi</h2>
                    <p>Hesapix aboneliğinizin süresi dolmuştur.</p>
                    <p>Premium özelliklerimize erişiminiz kısıtlanmıştır. Kesintisiz hizmet için lütfen aboneliğinizi yenileyin.</p>
                    <a href='https://hesapix.com/subscription' class='button'>Aboneliğimi Yenile</a>
                    <p style='margin-top: 30px;'>Teşekkürler,<br/>Hesapix Ekibi</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionCancelledEmailAsync(string toEmail)
    {
        var subject = "Hesapix - Aboneliğiniz İptal Edildi";
        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>Aboneliğiniz İptal Edildi</h2>
                    <p>Hesapix aboneliğiniz başarıyla iptal edilmiştir.</p>
                    <p>Mevcut dönem sonuna kadar hizmetlerimizden yararlanmaya devam edebilirsiniz.</p>
                    <p style='margin-top: 30px;'>Bizi tercih ettiğiniz için teşekkür ederiz,<br/>Hesapix Ekibi</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionRenewalReminderAsync(string toEmail, Subscription subscription)
    {
        var daysRemaining = (subscription.EndDate - DateTime.UtcNow).Days;
        var subject = $"Hesapix - Aboneliğiniz {daysRemaining} Gün İçinde Sona Eriyor";

        var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .reminder {{ color: #ffc107; }}
                    .button {{ 
                        display: inline-block; 
                        padding: 12px 24px; 
                        margin: 20px 0; 
                        background-color: #28a745; 
                        color: white !important; 
                        text-decoration: none; 
                        border-radius: 4px; 
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2 class='reminder'>🔔 Abonelik Hatırlatması</h2>
                    <p>Hesapix aboneliğinizin sona ermesine <strong>{daysRemaining} gün</strong> kaldı.</p>
                    <p><strong>Bitiş Tarihi:</strong> {subscription.EndDate:dd.MM.yyyy}</p>
                    <p>Kesintisiz hizmet almak için lütfen aboneliğinizi yenileyin.</p>
                    <a href='https://hesapix.com/subscription' class='button'>Aboneliğimi Yenile</a>
                    <p style='margin-top: 30px;'>Teşekkürler,<br/>Hesapix Ekibi</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }
}