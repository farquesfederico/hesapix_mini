using Hesapix.Models.Enums;

namespace Hesapix.Models.DTOs.Sale;

public class CreateSaleRequest
{
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxNumber { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    public decimal TaxRate { get; set; } = 18;
    public decimal DiscountAmount { get; set; } = 0;
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public List<SaleItemRequest> Items { get; set; } = new();
}

public class SaleItemRequest
{
    public int StockId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}