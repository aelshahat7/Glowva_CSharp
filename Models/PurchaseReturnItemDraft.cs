namespace GlowvaERP.Models;

public sealed class PurchaseReturnItemDraft
{
    public long PurchaseItemId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal PurchasedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AlreadyReturned { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal AvailableToReturn => Math.Max(0m, PurchasedQuantity - AlreadyReturned);
    public decimal Total => ReturnQuantity * UnitPrice;
}
