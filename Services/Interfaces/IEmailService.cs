namespace Hesapix.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string name, string verificationCode);
        Task SendPasswordResetEmailAsync(string email, string name, string resetToken);
        Task SendInvoiceEmailAsync(string email, string name, byte[] pdfBytes, string invoiceNumber);
    }
}