namespace GlowvaERP.Services;

public static class PaymentMethods
{
    public const string Cash = "نقدي";
    public const string Credit = "آجل";

    public static bool IsImmediate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var text = value.Trim();
        return text.Equals(Cash, StringComparison.OrdinalIgnoreCase)
            || text.Equals("كاش", StringComparison.OrdinalIgnoreCase)
            || text.Equals("نقد", StringComparison.OrdinalIgnoreCase)
            || text.Equals("cash", StringComparison.OrdinalIgnoreCase)
            || text.Equals("paid", StringComparison.OrdinalIgnoreCase)
            || text.Equals("مدفوع", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();
        return IsImmediate(text)
            || text.Equals(Credit, StringComparison.OrdinalIgnoreCase)
            || text.Equals("آجل", StringComparison.OrdinalIgnoreCase)
            || text.Equals("اجل", StringComparison.OrdinalIgnoreCase)
            || text.Equals("credit", StringComparison.OrdinalIgnoreCase)
            || text.Equals("on credit", StringComparison.OrdinalIgnoreCase);
    }
}
