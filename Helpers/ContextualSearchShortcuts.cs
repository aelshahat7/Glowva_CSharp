using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Global contextual lookup. F1 follows the control that currently has focus.
/// Customer/supplier selectors also open their matching selector on double-click.
/// </summary>
public static class ContextualSearchShortcuts
{
    private const int WmKeyDown = 0x0100;
    private static bool _installed;
    private static readonly HashSet<ComboBox> Wired = new();

    public static void InstallGlobal()
    {
        if (_installed)
            return;

        _installed = true;
        Application.AddMessageFilter(new Filter());
        Application.Idle += (_, _) => WireOpenForms();
        WireOpenForms();
    }

    private static void WireOpenForms()
    {
        foreach (Form form in Application.OpenForms)
        {
            foreach (var combo in GetAllControls(form).OfType<ComboBox>())
            {
                if (Wired.Contains(combo) || !string.Equals(combo.ValueMember, "Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                Wired.Add(combo);
                combo.DoubleClick += PartyCombo_DoubleClick;
            }
        }
    }

    private static void PartyCombo_DoubleClick(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo || combo.FindForm() is not Form form)
            return;

        if (!TryGetPartyMode(form, combo, out var supplierMode))
            return;

        OpenPartySearch(form, combo, supplierMode);
    }

    private sealed class Filter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmKeyDown || m.WParam.ToInt32() != (int)Keys.F1 || Control.ModifierKeys != Keys.None)
                return false;

            var active = Form.ActiveForm;
            var form = ResolveOwningForm(m.HWnd) ?? FindFocusedEmbeddedForm(active) ?? active;
            if (form is null || !form.Visible)
                return false;

            WireOpenForms();
            var focused = FindFocusedControl(form);

            if (focused is ComboBox combo && TryGetPartyMode(form, combo, out var supplierMode))
            {
                OpenPartySearch(form, combo, supplierMode);
                return true;
            }

            if (focused is TextBox textBox && IsProductSearchBox(textBox))
            {
                OpenProductSearch(form, textBox);
                return true;
            }

            if (IsTransactionScreen(form))
            {
                OpenProductSearch(form, FindProductSearchBox(form));
                return true;
            }

            return false;
        }
    }

    private static void OpenPartySearch(Form form, ComboBox combo, bool supplierMode)
    {
        using var dialog = new PartySearchDialog(supplierMode, combo.Text?.Trim());
        if (dialog.ShowDialog(form) != DialogResult.OK || !dialog.SelectedId.HasValue)
            return;

        try
        {
            combo.SelectedValue = dialog.SelectedId.Value;
        }
        catch
        {
            combo.Text = dialog.SelectedName ?? combo.Text;
        }
    }

    private static void OpenProductSearch(Form form, TextBox? productBox)
    {
        using var dialog = new ProductSearchDialog(productBox?.Text.Trim() ?? string.Empty);
        if (dialog.ShowDialog(form) != DialogResult.OK || dialog.SelectedProductName is null)
            return;

        productBox ??= FindProductSearchBox(form);
        if (productBox is null)
            return;

        productBox.Text = dialog.SelectedProductName;
        productBox.Focus();
        productBox.SelectionStart = productBox.TextLength;
    }

    private static bool TryGetPartyMode(Form form, ComboBox combo, out bool supplierMode)
    {
        var hint = string.Join(" ", new[]
        {
            form.GetType().Name,
            combo.Name,
            combo.AccessibleName ?? string.Empty,
            FindAdjacentLabel(combo)?.Text ?? string.Empty
        }).ToLowerInvariant();

        supplierMode = hint.Contains("supplier") || hint.Contains("purchase") || hint.Contains("مورد") || hint.Contains("مشتريات");
        if (supplierMode)
            return true;

        if (hint.Contains("customer") || hint.Contains("sales") || hint.Contains("عميل") || hint.Contains("مبيعات"))
        {
            supplierMode = false;
            return true;
        }

        return false;
    }

    private static Label? FindAdjacentLabel(ComboBox combo)
    {
        var parent = combo.Parent;
        if (parent is null)
            return null;

        var index = parent.Controls.IndexOf(combo);
        for (var i = index - 1; i >= 0; i--)
        {
            if (parent.Controls[i] is Label label)
                return label;
        }

        return parent.Controls.OfType<Label>().FirstOrDefault();
    }

    private static bool IsTransactionScreen(Form form)
    {
        var name = form.GetType().Name;
        return name.Contains("Sales", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Purchase", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Return", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("مرتجع", StringComparison.OrdinalIgnoreCase);
    }

    private static Form? ResolveOwningForm(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        var control = Control.FromHandle(hwnd);
        while (control is not null)
        {
            if (control is Form form)
                return form;
            control = control.Parent;
        }

        return null;
    }

    private static Form? FindFocusedEmbeddedForm(Form? activeTopLevel)
    {
        if (activeTopLevel is null)
            return null;

        return GetAllControls(activeTopLevel).OfType<Form>().FirstOrDefault(f => f.Visible && f.ContainsFocus);
    }

    private static TextBox? FindProductSearchBox(Form form) =>
        GetAllControls(form).OfType<TextBox>().FirstOrDefault(IsProductSearchBox);

    private static bool IsProductSearchBox(TextBox textBox) =>
        textBox.PlaceholderText.Contains("الصنف", StringComparison.OrdinalIgnoreCase) &&
        (textBox.PlaceholderText.Contains("الكود", StringComparison.OrdinalIgnoreCase) ||
         textBox.PlaceholderText.Contains("الباركود", StringComparison.OrdinalIgnoreCase));

    private static Control? FindFocusedControl(Control root)
    {
        if (root.Focused)
            return root;

        foreach (Control child in root.Controls)
        {
            if (!child.ContainsFocus)
                continue;
            var found = FindFocusedControl(child);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static IEnumerable<Control> GetAllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in GetAllControls(child))
                yield return nested;
        }
    }
}
