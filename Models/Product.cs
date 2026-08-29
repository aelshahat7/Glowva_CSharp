namespace GlowvaERP.Models;

public sealed class Product
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal SellPrice { get; set; }
    public decimal BuyPrice { get; set; }
    public decimal OpeningStock { get; set; }
    public decimal LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}
