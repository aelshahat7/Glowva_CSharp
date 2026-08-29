namespace GlowvaERP.Models;

public sealed class InventoryStock
{
    public long ProductId { get; set; }
    public string Code { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal OpeningStock { get; set; }
    public decimal Purchased { get; set; }
    public decimal Sold { get; set; }
    public decimal PurchaseReturns { get; set; }
    public decimal SalesReturns { get; set; }
    public decimal CurrentStock => OpeningStock + Purchased - Sold - PurchaseReturns + SalesReturns;
    public decimal LowStockThreshold { get; set; }
    public bool IsLowStock => CurrentStock <= LowStockThreshold;
}
