using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Legacy reference visual layer for the supplied pharmacy ERP screenshot.
/// Presentation only: no database/schema changes and no sales/inventory logic changes.
/// </summary>
public static class LegacyReferenceUi
{
    private static readonly Color Gold = Color.FromArgb(255, 204, 74);
    private static readonly Color Green = Color.FromArgb(0, 126, 25);
    private static readonly Color Blue = Color.FromArgb(45, 121, 185);
    private static readonly Color Orange = Color.FromArgb(230, 126, 34);
    private static readonly Color Red = Color.FromArgb(192, 57, 43);
    private static readonly Color Purple = Color.FromArgb(123, 31, 162);
    private static readonly Color Teal = Color.FromArgb(0, 153, 153);
    private static readonly Color Border = Color.FromArgb(150, 150, 150);
    private static readonly Color LightGreen = Color.FromArgb(244, 249, 244);

    public static void Apply(Form form)
    {
        if (form is WorkspaceShellForm workspace)
        {
            ApplyWorkspace(workspace);
            return;
        }

        if (form is SalesForm sales)
            ApplySalesForm(sales);
    }

    private static void ApplyWorkspace(WorkspaceShellForm form)
    {
        form.RightToLeftLayout = false;
        form.BackColor = Color.White;
        form.ForeColor = Color.Black;

        var menu = form.MainMenuStrip;
        if (menu != null)
        {
            menu.Dock = DockStyle.Top;
            menu.Height = 31;
            menu.BackColor = Gold;
            menu.ForeColor = Color.Black;
            menu.Font = new Font("Tahoma", 9.5F, FontStyle.Bold);
            menu.Padding = new Padding(3, 0, 3, 0);
            menu.RenderMode = ToolStripRenderMode.System;
            menu.RightToLeft = RightToLeft.Yes;
        }

        var rail = FindMainRail(form);
        if (rail != null)
        {
            rail.Width = 84;
            rail.BackColor = Gold;
            BuildReferenceModuleRail(form, rail);
        }
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
        host.RowCount = 8;
        host.Dock = DockStyle.Fill;
        host.BackColor = Gold;
        host.Padding = Padding.Empty;
        host.Margin = Padding.Empty;
        host.RightToLeft = RightToLeft.Yes;
        host.RowStyles.Clear();

        for (var i = 0; i < 7; i++)
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        AddModuleButton(host, 0, "مراجعة فواتير", "▣", Color.FromArgb(238, 238, 238), Blue, () => ShowPlaceholder(form, "مراجعة فواتير"));
        AddModuleButton(host, 1, "النواقص", "⚑", Color.FromArgb(245, 146, 20), Color.White, () => ShowPlaceholder(form, "النواقص"));
        AddModuleButton(host, 2, "الأصناف", "◉", Color.FromArgb(38, 140, 62), Color.White, () => form.OpenChild(() => new ProductsForm(), "الأصناف"));
        AddModuleButton(host, 3, "المشتريات", "▣", Teal, Color.White, () => form.OpenChild(() => new PurchasesForm(), "المشتريات"));
        AddModuleButton(host, 4, "المبيعات", "▣", Color.FromArgb(33, 150, 243), Color.White, () => form.OpenChild(() => new SalesForm(), "المبيعات"));
        AddModuleButton(host, 5, "توريد نقدي", "₪", Purple, Color.White, () => ShowPlaceholder(form, "توريد نقدي"));
        AddModuleButton(host, 6, "العملاء", "♟", Color.FromArgb(41, 128, 185), Color.White, () => form.OpenChild(() => new PartyListForm(false), "العملاء"));

        host.ResumeLayout(true);
    }

    private static void AddModuleButton(TableLayoutPanel host, int row, string label, string icon, Color backColor, Color foreColor, Action action)
    {
        var button = new Button
        {
            Text = $"{icon}\r\n{label}",
            Dock = DockStyle.Fill,
            Margin = new Padding(1),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = foreColor,
            Font = new Font("Tahoma", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            RightToLeft = RightToLeft.Yes,
            TabStop = false,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = backColor;
        button.FlatAppearance.MouseDownBackColor = backColor;
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
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
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
        rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        for (var i = 1; i < 8; i++)
            rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        rail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var buttons = rail.Controls.OfType<Button>().OrderBy(b => rail.GetRow(b)).ToArray();
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

        var close = rail.Controls.OfType<Button>().OrderBy(b => rail.GetRow(b)).Skip(8).FirstOrDefault();
        if (close != null)
        {
            close.Text = "✕\r\nإغلاق";
            close.BackColor = Red;
            close.ForeColor = Color.White;
            close.Font = new Font("Tahoma", 8.5F, FontStyle.Bold);
            close.FlatStyle = FlatStyle.Flat;
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Red;
            close.FlatAppearance.MouseDownBackColor = Red;
            close.UseVisualStyleBackColor = false;
            close.TextAlign = ContentAlignment.MiddleCenter;
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
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));

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
            StyleControls(headerLayout);

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
                label.Font = new Font("Tahoma", 8.5F, label.Text.Any(char.IsDigit) ? FontStyle.Bold : FontStyle.Regular);
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
            total.ForeColor = Green;
            total.TextAlign = ContentAlignment.MiddleRight;
        }
    }

    private static void ShowPlaceholder(Form owner, string featureName)
    {
        MessageBox.Show(
            owner,
            $"وظيفة \"{featureName}\" سيتم تنفيذها لاحقًا.",
            "Glowva ERP",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
