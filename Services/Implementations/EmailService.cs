using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Hesapix.Services.Interfaces;

namespace Hesapix.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email, string name, string verificationCode)
        {
            var subject = "Hesapix - Email Doğrulama";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Merhaba {name},</h2>
                    <p>Hesapix'e hoş geldiniz! Email adresinizi doğrulamak için aşağıdaki kodu kullanın:</p>
                    <h1 style='color: #4CAF50; letter-spacing: 5px;'>{verificationCode}</h1>
                    <p>Bu kod 24 saat geçerlidir.</p>
                    <p>Eğer bu işlemi siz yapmadıysanız, lütfen bu emaili görmezden gelin.</p>
                    <br>
                    <p>Hesapix Ekibi</p>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string email, string name, string resetToken)
        {
            var resetUrl = $"{_configuration["AppSettings:FrontendUrl"]}/reset-password?token={resetToken}";

            var subject = "Hesapix - Şifre Sıfırlama";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Merhaba {name},</h2>
                    <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
                    <p><a href='{resetUrl}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Şifremi Sıfırla</a></p>
                    <p>Bu link 1 saat geçerlidir.</p>
                    <p>Eğer şifre sıfırlama talebinde bulunmadıysanız, lütfen bu emaili görmezden gelin.</p>
                    <br>
                    <p>Hesapix Ekibi</p>
                </body>
                </html>
            ";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendInvoiceEmailAsync(string email, string name, byte[] pdfBytes, string invoiceNumber)
        {
            var subject = $"Hesapix - Fatura #{invoiceNumber}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2>Merhaba {name},</h2>
                    <p>Faturanız ektedir.</p>
                    <p><strong>Fatura No:</strong> {invoiceNumber}</p>
                    <br>
                    <p>İyi günler dileriz.</p>
                    <p>Hesapix Ekibi</p>
                </body>
                </html>
            ";

            var message = new MimeMessage();

            var fromName = _configuration["Email:FromName"] ?? "Hesapix";
            var fromAddress = _configuration["Email:Username"];

            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(name, email)); // Alıcının adı ve emaili
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };

            // PDF eklentisi
            builder.Attachments.Add($"Fatura_{invoiceNumber}.pdf", pdfBytes, ContentType.Parse("application/pdf"));

            message.Body = builder.ToMessageBody();

            await SendEmailInternalAsync(message);
        }

        private async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var message = new MimeMessage();

            var fromName = _configuration["Email:FromName"] ?? "Hesapix";
            var fromAddress = _configuration["Email:Username"]; // Gmail username'i gönderici adresi olarak kullan

            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress("", to)); // Alıcı adresi
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body = builder.ToMessageBody();

            await SendEmailInternalAsync(message);
        }

        private async Task SendEmailInternalAsync(MimeMessage message)
        {
            try
            {
                using var client = new SmtpClient();

                var host = _configuration["Email:SmtpHost"];
                var port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];

                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", message.To);
                throw;
            }
        }
    }
}