namespace GlowvaERP.Models;

public sealed class SalesOrderItemDraft
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
}
