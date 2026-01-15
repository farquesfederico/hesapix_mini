namespace Hesapix.Models.Entities;

public class Stok
{
    public int Id { get; set; }
    public int UserId { get; set; }
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
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}