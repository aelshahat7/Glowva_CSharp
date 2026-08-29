namespace GlowvaERP.Models;

public sealed class SalesReturnItemDraft
{
    public long OrderItemId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal SoldQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal AlreadyReturned { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal AvailableToReturn => Math.Max(0m, SoldQuantity - AlreadyReturned);
    public decimal Total => ReturnQuantity * UnitPrice;
}
