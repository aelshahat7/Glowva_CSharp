using System.Windows.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Central keyboard shortcut definitions. F1 is intentionally not consumed by individual
/// forms because contextual search is handled by ContextualSearchShortcuts globally.
/// </summary>
public static class KeyboardShortcuts
{
    public static bool IsProductSearch(Keys keyData) => false;

    public static bool IsProductCard(Keys keyData) => keyData == Keys.F3;

    public static bool IsDeleteRow(Keys keyData) => keyData == Keys.Delete;

    public static bool IsSave(Keys keyData) => keyData == Keys.F8;

    public static bool IsCancel(Keys keyData) => keyData == Keys.Escape;
}
