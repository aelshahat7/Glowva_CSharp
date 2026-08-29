namespace GlowvaERP.Models;

/// <summary>
/// طرق الدفع المعتمدة في البرنامج، موحّدة في مكان واحد.
/// </summary>
public static class PaymentMethods
{
    public const string Cash = "كاش";
    public const string Visa = "فيزا";
    public const string Credit = "آجل";
    public const string ElectronicWallet = "محفظة إلكترونية";
    public const string BankTransfer = "تحويل بنكي";

    public static IReadOnlyList<string> All { get; } =
    [
        Cash,
        Visa,
        Credit,
        ElectronicWallet,
        BankTransfer
    ];

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static bool IsImmediate(string? value) =>
        value is Cash or Visa or ElectronicWallet or BankTransfer;
}
