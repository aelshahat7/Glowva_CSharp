using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace GlowvaERP.Helpers;

internal static class ReferenceWindowRuntimeFix
{
    public static void Apply(Form form)
    {
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = false;
        form.AutoScaleMode = AutoScaleMode.None;
        form.Font = new Font("Tahoma", 9.5F, FontStyle.Regular);

        if (form is GlowvaERP.Forms.OperatingSettingsForm)
            FixOperatingSettings(form);

        if (form is GlowvaERP.Forms.BarcodePrintSettingsForm)
            FixBarcodePrintSettings(form);

        if (form is GlowvaERP.Forms.SalesInvoicePrintSettingsForm)
            FixInvoicePrintSettings(form);

        PopulatePrinterCombos(form);
    }

    private static void FixOperatingSettings(Form form)
    {
        form.ClientSize = new Size(500, 670);
        foreach (var flow in FindControls<FlowLayoutPanel>(form))
        {
            flow.RightToLeft = RightToLeft.No;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoScroll = true;
            flow.Dock = DockStyle.Fill;
            flow.Padding = new Padding(10, 8, 10, 0);
            foreach (Control child in flow.Controls)
            {
                child.RightToLeft = RightToLeft.No;
                if (child is Panel section)
                {
                    section.Width = Math.Max(450, flow.ClientSize.Width - 24);
                    section.RightToLeft = RightToLeft.No;
                }
            }
        }
    }

    private static void FixBarcodePrintSettings(Form form)
    {
        form.ClientSize = new Size(580, 590);
        var preview = FindControls<Label>(form)
            .FirstOrDefault(x => x.Width >= 250 && x.Height >= 70 && x.BorderStyle == BorderStyle.FixedSingle);
        if (preview == null)
            return;

        var textBoxes = FindControls<TextBox>(form).ToArray();
        var checks = FindControls<CheckBox>(form).ToArray();
        var printer = FindControls<ComboBox>(form).FirstOrDefault(IsLikelyPrinterCombo);
        var org = textBoxes.FirstOrDefault(x => string.Equals(x.Text, "صيدلية ياسين", StringComparison.Ordinal));
        var phone = textBoxes.FirstOrDefault(x => string.Equals(x.Text, "0502338665", StringComparison.Ordinal));
        var noBarcode = checks.FirstOrDefault(x => x.Text.Contains("لا يوجد", StringComparison.Ordinal));
        var thermal = checks.FirstOrDefault(x => x.Text.Contains("حرارية", StringComparison.Ordinal));
        var a4 = checks.FirstOrDefault(x => x.Text.Contains("A4", StringComparison.OrdinalIgnoreCase));
        void UpdatePreview()
        {
            if (noBarcode?.Checked == true)
            {
                preview.Text = "لا يوجد طباعة باركود";
                return;
            }
            var orgText = org != null ? org.Text : "";
            var phoneText = phone != null ? phone.Text : "";
            var mode = a4?.Checked == true ? "A4" : thermal?.Checked == true ? "حرارية" : "";
            var printerName = printer?.SelectedItem?.ToString() ?? "";
            preview.Text = $"{phoneText}      {orgText}\r\n\r\n||||||||||||||||||||||||||||||\r\nاسم الصنف مكتوب هنا       4.00 E.L.\r\n\r\n{mode}  |  {printerName}";
        }

        if (org != null) org.TextChanged += (_, _) => UpdatePreview();
        if (phone != null) phone.TextChanged += (_, _) => UpdatePreview();
        if (noBarcode != null) noBarcode.CheckedChanged += (_, _) => UpdatePreview();
        if (thermal != null) thermal.CheckedChanged += (_, _) => UpdatePreview();
        if (a4 != null) a4.CheckedChanged += (_, _) => UpdatePreview();
        if (printer != null) printer.SelectedIndexChanged += (_, _) => UpdatePreview();
        UpdatePreview();
    }

    private static void FixInvoicePrintSettings(Form form)
    {
        form.ClientSize = new Size(1000, 450);
        foreach (var table in FindControls<TableLayoutPanel>(form))
            table.RightToLeft = RightToLeft.No;

        PopulatePrinterCombos(form);
    }

    private static void PopulatePrinterCombos(Form form)
    {
        string[] printers;
        try
        {
            printers = PrinterSettings.InstalledPrinters.Cast<string>()
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            printers = Array.Empty<string>();
        }

        foreach (var combo in FindControls<ComboBox>(form).Where(IsLikelyPrinterCombo))
        {
            var current = combo.SelectedItem?.ToString() ?? combo.Text;
            combo.Items.Clear();
            if (printers.Length == 0)
            {
                combo.Items.Add("لا توجد طابعات معرفة");
                combo.SelectedIndex = 0;
                continue;
            }

            combo.Items.AddRange(printers);
            var match = printers.FirstOrDefault(x => string.Equals(x, current, StringComparison.OrdinalIgnoreCase));
            combo.SelectedItem = match ?? printers[0];
        }
    }

    private static bool IsLikelyPrinterCombo(ComboBox combo)
    {
        var parent = combo.Parent;
        while (parent != null)
        {
            foreach (Control sibling in parent.Controls)
            {
                if (sibling is Label label && label.Text.Contains("طابعة", StringComparison.Ordinal))
                    return true;
            }
            parent = parent.Parent;
        }
        return combo.Items.Cast<object>().Any(x => x?.ToString()?.Contains("Printer", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static IEnumerable<T> FindControls<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T match)
                yield return match;
            foreach (var nested in FindControls<T>(child))
                yield return nested;
        }
    }
}
