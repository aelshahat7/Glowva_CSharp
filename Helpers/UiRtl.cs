using System.Windows.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Applies a consistent Arabic right-to-left layout across application forms and controls.
/// Explicit container directions are preserved so the workspace shell can keep its right rail.
/// </summary>
public static class UiRtl
{
    private static readonly HashSet<Form> InstalledForms = new();

    public static void InstallGlobal()
    {
        Application.Idle += (_, _) => ApplyToOpenForms();
        ApplyToOpenForms();
    }

    private static void ApplyToOpenForms()
    {
        foreach (Form form in Application.OpenForms)
        {
            if (InstalledForms.Contains(form))
                continue;

            InstalledForms.Add(form);
            ApplyToForm(form);
        }
    }

    public static void ApplyToForm(Form form)
    {
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = true;

        WireControlTree(form);
        ApplyTree(form);
        ScreenLayout.Apply(form);
    }

    private static void WireControlTree(Control root)
    {
        root.ControlAdded -= ControlAdded;
        root.ControlAdded += ControlAdded;

        foreach (Control child in root.Controls)
            WireControlTree(child);
    }

    private static void ControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is null)
            return;
        WireControlTree(e.Control);
        ApplyTree(e.Control);
    }

    private static void ApplyTree(Control root)
    {
        ApplyControl(root);

        foreach (Control child in root.Controls)
            ApplyTree(child);
    }

    private static void ApplyControl(Control control)
    {
        if (control is Form)
            return;

        // Do not overwrite explicit TableLayoutPanel/FlowLayoutPanel directions.
        // Their RightToLeft setting controls the physical order of the shell rails.
        if (control is not TableLayoutPanel && control is not FlowLayoutPanel)
            control.RightToLeft = RightToLeft.Yes;

        if (control is DataGridView grid)
            grid.RightToLeft = RightToLeft.Yes;

        if (control is ComboBox combo)
            combo.RightToLeft = RightToLeft.Yes;

        if (control is TextBox textBox)
            textBox.RightToLeft = RightToLeft.Yes;

        if (control is NumericUpDown numeric)
            numeric.TextAlign = HorizontalAlignment.Right;

        if (control is DateTimePicker picker)
        {
            picker.RightToLeft = RightToLeft.Yes;
            picker.RightToLeftLayout = false;
        }
    }
}
