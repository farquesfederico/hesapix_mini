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
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                emailSettings["SmtpServer"],
                int.Parse(emailSettings["SmtpPort"]!),
                SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                emailSettings["Username"],
                emailSettings["Password"]
            );

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

    public async Task<bool> SendVerificationEmailAsync(string toEmail, string verificationLink)
    {
        var subject = "Hesapix - Email Doğrulama";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #333;'>Hoş Geldiniz!</h2>
                    <p>Hesapix'e kayıt olduğunuz için teşekkür ederiz.</p>
                    <p>Lütfen aşağıdaki bağlantıya tıklayarak email adresinizi doğrulayın:</p>
                    <a href='{verificationLink}' 
                       style='display: inline-block; padding: 12px 24px; margin: 20px 0; 
                              background-color: #007bff; color: white; text-decoration: none; 
                              border-radius: 4px;'>
                        Email Adresimi Doğrula
                    </a>
                    <p style='color: #666; font-size: 14px;'>
                        Bu link 24 saat geçerlidir. Eğer bu işlemi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.
                    </p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var subject = "Hesapix - Şifre Sıfırlama";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #333;'>Şifre Sıfırlama Talebi</h2>
                    <p>Şifrenizi sıfırlamak için bir talepte bulundunuz.</p>
                    <p>Lütfen aşağıdaki bağlantıya tıklayarak yeni şifrenizi belirleyin:</p>
                    <a href='{resetLink}' 
                       style='display: inline-block; padding: 12px 24px; margin: 20px 0; 
                              background-color: #dc3545; color: white; text-decoration: none; 
                              border-radius: 4px;'>
                        Şifremi Sıfırla
                    </a>
                    <p style='color: #666; font-size: 14px;'>
                        Bu link 1 saat geçerlidir. Eğer bu işlemi siz yapmadıysanız, lütfen bu emaili görmezden geçin ve hesap güvenliğinizi kontrol edin.
                    </p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionConfirmationEmailAsync(string toEmail, string companyName, DateTime endDate)
    {
        var subject = "Hesapix - Abonelik Onayı";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #28a745;'>Aboneliğiniz Aktif!</h2>
                    <p>Sayın {companyName},</p>
                    <p>Hesapix aboneliğiniz başarıyla aktif edilmiştir.</p>
                    <p><strong>Abonelik Bitiş Tarihi:</strong> {endDate:dd.MM.yyyy}</p>
                    <p>Tüm özelliklerimizden faydalanabilirsiniz.</p>
                    <p style='margin-top: 20px;'>İyi çalışmalar dileriz!</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendSubscriptionExpirationWarningAsync(string toEmail, string companyName, int daysRemaining)
    {
        var subject = "Hesapix - Abonelik Hatırlatması";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <h2 style='color: #ffc107;'>Abonelik Hatırlatması</h2>
                    <p>Sayın {companyName},</p>
                    <p>Hesapix aboneliğinizin sona ermesine <strong>{daysRemaining} gün</strong> kaldı.</p>
                    <p>Kesintisiz hizmet almak için lütfen aboneliğinizi yenileyin.</p>
                    <p style='margin-top: 20px;'>Teşekkürler,<br/>Hesapix Ekibi</p>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(toEmail, subject, body);
    }
}