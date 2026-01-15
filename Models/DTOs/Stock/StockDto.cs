namespace Hesapix.Models.DTOs.Stock;

public class StockDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public int Quantity { get; set; }
    public int MinimumStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? TaxRate { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLowStock => Quantity <= MinimumStock;
}

public class CreateStockRequest
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public int Quantity { get; set; }
    public int MinimumStock { get; set; } = 0;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal? TaxRate { get; set; } = 18;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
}

public class UpdateStockRequest : CreateStockRequest
{
}