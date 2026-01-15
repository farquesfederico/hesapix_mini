using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Payment;

public class PaymentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Description { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}