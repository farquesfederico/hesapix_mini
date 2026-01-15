using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Payment;

public class CreatePaymentRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? InvoiceNumber { get; set; }
}