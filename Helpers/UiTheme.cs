using System.Drawing;
using System.Windows.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Shared visual language for the ERP: compact Arabic typography, clean surfaces,
/// consistent controls, and readable data grids. Layout ownership remains with each screen.
/// </summary>
public static class UiTheme
{
    private static readonly HashSet<Form> InstalledForms = new();
    public static readonly Color Surface = Color.White;
    public static readonly Color Page = Color.FromArgb(247, 248, 247);
    public static readonly Color Border = Color.FromArgb(204, 211, 204);
    public static readonly Color Text = Color.FromArgb(38, 43, 48);
    public static readonly Color Muted = Color.FromArgb(100, 108, 116);
    public static readonly Color Accent = Color.FromArgb(0, 129, 30);
    public static readonly Color InvoiceGreen = Color.FromArgb(0, 126, 25);
    public static readonly Color InvoiceBlue = Color.FromArgb(0, 52, 175);
    public static readonly Color ChromeGold = Color.FromArgb(255, 204, 74);
    public static readonly Color PrimaryAction = Color.FromArgb(35, 126, 61);
    public static readonly Color SecondaryAction = Color.FromArgb(45, 121, 185);
    public static readonly Color WarningAction = Color.FromArgb(230, 126, 34);
    public static readonly Color DangerAction = Color.FromArgb(192, 57, 43);
    public const int InputHeight = 32;
    public const int ButtonHeight = 36;

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
        form.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        form.BackColor = Page;
        form.ForeColor = Text;
        Wire(form);
        ApplyTree(form);
    }

    private static void Wire(Control root)
    {
        root.ControlAdded -= ControlAdded;
        root.ControlAdded += ControlAdded;
        foreach (Control child in root.Controls)
            Wire(child);
    }

    private static void ControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is null)
            return;
        Wire(e.Control);
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
        if (control is MenuStrip menu)
        {
            menu.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            menu.Padding = new Padding(8, 3, 8, 3);
            return;
        }
        if (control is StatusStrip status)
        {
            status.Font = new Font("Segoe UI", 8.5F);
            return;
        }
        if (control is DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.GridColor = Border;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 34);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Accent;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 242, 241);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
            grid.RowTemplate.Height = Math.Max(grid.RowTemplate.Height, 30);
            return;
        }
        if (control is Button button)
        {
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = button.BackColor == Color.Empty ? 1 : 0;
            button.FlatAppearance.BorderColor = Border;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Hand;
            return;
        }
        if (control is TextBox textBox)
        {
            textBox.BackColor = Surface;
            textBox.ForeColor = Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 9.5F);
            textBox.TextAlign = HorizontalAlignment.Right;
            return;
        }
        if (control is ComboBox combo)
        {
            combo.BackColor = Surface;
            combo.ForeColor = Text;
            combo.Font = new Font("Segoe UI", 9.5F);
            combo.FlatStyle = FlatStyle.Standard;
            return;
        }
        if (control is NumericUpDown numeric)
        {
            numeric.BackColor = Surface;
            numeric.ForeColor = Text;
            numeric.Font = new Font("Segoe UI", 9.5F);
            numeric.TextAlign = HorizontalAlignment.Right;
            return;
        }
        if (control is CheckBox check)
        {
            check.Font = new Font("Segoe UI", 9.5F);
            check.ForeColor = Text;
            return;
        }
        if (control is Label label)
        {
            if (label.Font.Size < 10F)
                label.Font = new Font("Segoe UI", 9F, label.Font.Style);
            if (label.ForeColor == Color.Black)
                label.ForeColor = Muted;
            return;
        }
        if (control is Panel or TableLayoutPanel or FlowLayoutPanel)
        {
            if (control.BackColor == SystemColors.Control || control.BackColor == Color.White)
                control.BackColor = Surface;
        }
    }

    public static void StyleInvoiceGrid(DataGridView grid, int rowHeight = 34)
    {
        grid.BackgroundColor = Surface;
        grid.GridColor = Color.FromArgb(182, 196, 182);
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = InvoiceGreen;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 241, 226);
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 252, 249);
        grid.RowTemplate.Height = rowHeight;
        grid.RowHeadersVisible = false;
        grid.ScrollBars = ScrollBars.Both;
    }

    public static void StyleInput(Control control)
    {
        control.Margin = new Padding(3);
        control.MinimumSize = new Size(0, InputHeight);
        control.BackColor = Surface;
        control.ForeColor = Text;
        control.Font = new Font("Segoe UI", 9F);
        switch (control)
        {
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.TextAlign = HorizontalAlignment.Right;
                break;
            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Standard;
                break;
            case NumericUpDown numeric:
                numeric.TextAlign = HorizontalAlignment.Right;
                break;
        }
    }

    public static void StyleActionButton(Button button, Color color)
    {
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        button.MinimumSize = new Size(0, ButtonHeight);
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }
}
