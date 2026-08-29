namespace GlowvaERP.Models;

public sealed class PurchaseOrderItemDraft
{
    public long ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; init; }

    public decimal Total => Quantity * UnitPrice;
}
