using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Runtime visual reconstruction for the supplied legacy ERP reference.
/// This class intentionally changes presentation only and does not modify data access or the database schema.
/// </summary>
public static class LegacyReferenceUi
{
    private static readonly Color Gold = Color.FromArgb(255, 204, 74);
    private static readonly Color Green = Color.FromArgb(0, 126, 25);
    private static readonly Color Blue = Color.FromArgb(45, 121, 185);
    private static readonly Color Orange = Color.FromArgb(230, 126, 34);
    private static readonly Color Red = Color.FromArgb(192, 57, 43);
    private static readonly Color LightGreen = Color.FromArgb(244, 249, 244);
    private static readonly Color Border = Color.FromArgb(150, 150, 150);

    private static bool _workspaceApplied;

    public static void Apply(Form form)
    {
        if (form is WorkspaceShellForm workspace)
        {
            ApplyWorkspace(workspace);
            return;
        }

        if (form is SalesForm sales)
        {
            ApplySalesForm(sales);
        }
    }

    private static void ApplyWorkspace(WorkspaceShellForm form)
    {
        if (_workspaceApplied && form.IsDisposed)
            return;

        // The reference uses classic Windows chrome with native caption buttons on the right.
        form.RightToLeftLayout = false;
        form.BackColor = Color.White;
        form.ForeColor = Color.Black;
        TryStyleNativeCaption(form);

        var menu = form.MainMenuStrip;
        if (menu != null)
        {
            menu.Dock = DockStyle.Top;
            menu.Height = 31;
            menu.BackColor = Gold;
            menu.ForeColor = Color.Black;
            menu.Font = new Font("Tahoma", 9.5F, FontStyle.Bold);
            menu.Padding = new Padding(4, 1, 4, 1);
            menu.RenderMode = ToolStripRenderMode.System;
            menu.RightToLeft = RightToLeft.Yes;
        }

        var mdi = form.Controls.OfType<MdiClient>().FirstOrDefault();
        if (mdi != null)
        {
            mdi.BackColor = Color.White;
            mdi.BorderStyle = BorderStyle.None;
        }

        var rail = FindMainRail(form);
        if (rail != null)
        {
            rail.Width = 80;
            rail.BackColor = Gold;
            BuildReferenceModuleRail(form, rail);
        }

        _workspaceApplied = true;
    }

    private static Panel? FindMainRail(WorkspaceShellForm form)
    {
        return form.Controls.OfType<Panel>()
            .OrderByDescending(p => p.Left)
            .FirstOrDefault(p => p.BackColor == UiTheme.ChromeGold || p.Width <= 90);
    }

    private static void BuildReferenceModuleRail(WorkspaceShellForm form, Panel rail)
    {
        var host = rail.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (host == null)
            return;

        host.SuspendLayout();
        host.Controls.Clear();
        host.ColumnCount = 1;
        host.RowCount = 6;
        host.Dock = DockStyle.Fill;
        host.BackColor = Gold;
        host.Padding = Padding.Empty;
        host.Margin = Padding.Empty;
        host.RightToLeft = RightToLeft.Yes;
        host.RowStyles.Clear();
        for (var i = 0; i < 6; i++)
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6F));

        AddModuleButton(host, 0, "النواقص", "⚑", Color.FromArgb(243, 156, 18), () => ShowPlaceholder(form, "النواقص"));
        AddModuleButton(host, 1, "الأصناف", "◉", Color.FromArgb(34, 139, 34), () => form.OpenChild(() => new ProductsForm(), "الأصناف"));
        AddModuleButton(host, 2, "المشتريات", "▣", Color.FromArgb(0, 153, 153), () => form.OpenChild(() => new PurchasesForm(), "المشتريات"));
        AddModuleButton(host, 3, "المبيعات", "▣", Color.FromArgb(33, 150, 243), () => form.OpenChild(() => new SalesForm(), "المبيعات"));
        AddModuleButton(host, 4, "توريد نقدي", "₪", Color.FromArgb(156, 39, 176), () => ShowPlaceholder(form, "توريد نقدي"));
        AddModuleButton(host, 5, "العملاء", "♟", Color.FromArgb(41, 128, 185), () => form.OpenChild(() => new PartyListForm(false), "العملاء"));

        host.ResumeLayout(true);
    }

    private static void AddModuleButton(TableLayoutPanel host, int row, string label, string icon, Color color, Action action)
    {
        var button = new Button
        {
            Text = $"{icon}\r\n{label}",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 0, 1),
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Tahoma", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            RightToLeft = RightToLeft.Yes,
            TabStop = false,
            Cursor = Cursors.Hand,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = color;
        button.FlatAppearance.MouseDownBackColor = color;
        button.Click += (_, _) => action();
        host.Controls.Add(button, 0, row);
    }

    private static void ApplySalesForm(SalesForm form)
    {
        form.BackColor = Color.White;
        form.ForeColor = Color.Black;
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = true;
        form.KeyPreview = true;
        form.MinimumSize = new Size(1100, 680);

        var root = form.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (root == null)
            return;

        root.BackColor = Gold;
        root.Padding = Padding.Empty;
        root.Margin = Padding.Empty;
        root.RightToLeft = RightToLeft.No;
        root.ColumnStyles.Clear();
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var rail = root.GetControlFromPosition(0, 0) as TableLayoutPanel;
        if (rail != null)
            ApplySalesRail(rail);

        var content = root.GetControlFromPosition(1, 0) as TableLayoutPanel;
        if (content != null)
            ApplySalesContent(content);
    }

    private static void ApplySalesRail(TableLayoutPanel rail)
    {
        rail.BackColor = Gold;
        rail.Padding = new Padding(2, 3, 2, 3);
        rail.Margin = Padding.Empty;
        rail.RightToLeft = RightToLeft.Yes;
        rail.ColumnCount = 1;
        rail.RowCount = 9;
        rail.RowStyles.Clear();
        rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        for (var i = 1; i < 9; i++)
            rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

        var buttons = rail.Controls.OfType<Button>().OrderBy(b => GetRow(rail, b)).ToArray();
        var labels = new[]
        {
            ("✚\r\nجديد", Green),
            ("▣\r\nحفظ", Blue),
            ("↻\r\nإلغاء", Orange),
            ("＋\r\nسطر جديد", Green),
            ("−\r\nحذف سطر", Red),
            ("⌕\r\nبحث صنف", Green),
            ("▣\r\nبطاقة صنف", Blue),
            ("▤\r\nطباعة", Blue),
            ("✕\r\nإغلاق", Red),
        };

        for (var i = 0; i < buttons.Length && i < labels.Length; i++)
        {
            var button = buttons[i];
            button.Text = labels[i].Item1;
            button.BackColor = labels[i].Item2;
            button.ForeColor = Color.White;
            button.Font = new Font("Tahoma", 8.5F, FontStyle.Bold);
            button.Margin = new Padding(1);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = labels[i].Item2;
            button.FlatAppearance.MouseDownBackColor = labels[i].Item2;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.UseVisualStyleBackColor = false;
        }
    }

    private static void ApplySalesContent(TableLayoutPanel content)
    {
        content.BackColor = Color.White;
        content.Padding = new Padding(2, 0, 2, 0);
        content.Margin = Padding.Empty;
        content.RightToLeft = RightToLeft.Yes;
        content.RowStyles.Clear();
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));

        var title = content.GetControlFromPosition(0, 0) as Label;
        if (title != null)
        {
            title.BackColor = Color.White;
            title.ForeColor = Color.FromArgb(0, 52, 175);
            title.Font = new Font("Tahoma", 19F, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleCenter;
        }

        var header = content.GetControlFromPosition(0, 1);
        if (header is TableLayoutPanel headerLayout)
        {
            headerLayout.BackColor = Color.White;
            headerLayout.Padding = new Padding(1, 0, 1, 0);
            StyleControls(headerLayout);
        }

        var summary = content.GetControlFromPosition(0, 2);
        if (summary is TableLayoutPanel summaryLayout)
            StyleSummary(summaryLayout);

        var grid = content.GetControlFromPosition(0, 3) as DataGridView;
        if (grid != null)
            StyleGrid(grid);

        var bottom = content.GetControlFromPosition(0, 4) as TableLayoutPanel;
        if (bottom != null)
            StyleBottom(bottom);
    }

    private static void StyleControls(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is TableLayoutPanel nested)
            {
                StyleControls(nested);
                continue;
            }

            control.Font = new Font("Tahoma", 9F, control.Font.Style == FontStyle.Bold ? FontStyle.Bold : FontStyle.Regular);
            if (control is Label label)
            {
                label.ForeColor = Color.Black;
                label.TextAlign = ContentAlignment.MiddleRight;
                label.Padding = new Padding(0, 0, 4, 0);
            }
            else if (control is TextBox textBox)
            {
                textBox.Height = 25;
                textBox.Font = new Font("Tahoma", 9F);
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.BackColor = Color.White;
                textBox.ForeColor = Color.Black;
            }
            else if (control is ComboBox combo)
            {
                combo.Height = 25;
                combo.Font = new Font("Tahoma", 9F);
                combo.BackColor = Color.White;
                combo.ForeColor = Color.Black;
                combo.FlatStyle = FlatStyle.Standard;
            }
            else if (control is NumericUpDown numeric)
            {
                numeric.Height = 25;
                numeric.Font = new Font("Tahoma", 9F);
                numeric.BackColor = Color.White;
                numeric.ForeColor = Color.Black;
            }
            else if (control is Button button)
            {
                button.Font = new Font("Tahoma", 8.5F, FontStyle.Bold);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = Border;
                button.UseVisualStyleBackColor = false;
            }
        }
    }

    private static void StyleSummary(TableLayoutPanel summary)
    {
        summary.BackColor = Color.FromArgb(245, 248, 245);
        summary.Padding = new Padding(0, 1, 0, 1);
        summary.Margin = Padding.Empty;
        summary.RightToLeft = RightToLeft.Yes;
        foreach (Control control in summary.Controls)
        {
            if (control is Label label)
            {
                label.Font = new Font("Tahoma", label.Text is "" ? 8F : 8.5F, label.Text.Any(char.IsDigit) ? FontStyle.Bold : FontStyle.Regular);
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Margin = Padding.Empty;
                label.Padding = new Padding(1);
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.BackColor = label.Text.Any(char.IsDigit) ? LightGreen : Color.FromArgb(230, 235, 230);
                label.ForeColor = label.Text.Any(char.IsDigit) ? Color.FromArgb(0, 100, 0) : Color.Black;
            }
        }
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.GridColor = Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 29;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 128, 0);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 8.5F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Font = new Font("Tahoma", 8.5F);
        grid.DefaultCellStyle.ForeColor = Color.Black;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 240, 220);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 250);
        grid.RowTemplate.Height = 25;
        grid.RowHeadersVisible = false;
        grid.RightToLeft = RightToLeft.Yes;
        grid.ScrollBars = ScrollBars.Both;
    }

    private static void StyleBottom(TableLayoutPanel bottom)
    {
        bottom.BackColor = Color.FromArgb(247, 249, 247);
        bottom.Padding = new Padding(2, 2, 2, 1);
        bottom.Margin = Padding.Empty;
        foreach (Control control in bottom.Controls)
        {
            control.Font = new Font("Tahoma", 8.5F, FontStyle.Regular);
            if (control is Label label)
            {
                label.ForeColor = Color.Black;
                label.TextAlign = ContentAlignment.MiddleRight;
            }
            else if (control is TextBox textBox)
            {
                textBox.Font = new Font("Tahoma", 9F);
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is NumericUpDown numeric)
            {
                numeric.Font = new Font("Tahoma", 9F);
                numeric.Height = 24;
            }
            else if (control is Button button)
            {
                button.BackColor = Green;
                button.ForeColor = Color.White;
                button.Font = new Font("Tahoma", 8.5F, FontStyle.Bold);
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.UseVisualStyleBackColor = false;
            }
        }

        var total = bottom.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("الإجمالي:"));
        if (total != null)
        {
            total.Font = new Font("Tahoma", 16F, FontStyle.Bold);
            total.ForeColor = Color.FromArgb(0, 126, 25);
            total.TextAlign = ContentAlignment.MiddleRight;
        }

        var save = bottom.Controls.OfType<Button>().FirstOrDefault(b => b.Text.Contains("حفظ فاتورة البيع"));
        if (save != null)
            save.Visible = false;
    }

    private static int GetRow(TableLayoutPanel table, Control child)
    {
        return table.GetRow(child);
    }

    private static void ShowPlaceholder(Form owner, string text)
    {
        MessageBox.Show(owner, $"قسم {text} جاهز للواجهة وسيتم إضافة الوظيفة لاحقًا.", owner.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void TryStyleNativeCaption(Form form)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return;

            var hwnd = form.Handle;
            var white = ColorRef(Color.White);
            var black = ColorRef(Color.Black);
            var square = 1;
            DwmSetWindowAttribute(hwnd, 35, ref white, sizeof(uint));
            DwmSetWindowAttribute(hwnd, 36, ref black, sizeof(uint));
            DwmSetWindowAttribute(hwnd, 33, ref square, sizeof(int));
        }
        catch
        {
            // Native chrome is cosmetic only. Ignore unsupported Windows configurations.
        }
    }

    private static uint ColorRef(Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref uint value, int valueSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);
}
