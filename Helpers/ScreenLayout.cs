using System.Drawing;
using System.Windows.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Shared WinForms layout rules for ERP screens.
/// Keeps fixed headers/footers visible, gives grids their own scrollbars,
/// protects split containers on smaller screens, and applies the screen font consistently.
/// </summary>
public static class ScreenLayout
{
    private const int MinGridHeight = 180;
    private const int MinSplitPanel = 140;

    public static void Apply(Form form)
    {
        if (form.Controls.ContainsKey("__glowvaScreenLayoutInstalled"))
            return;

        form.Controls.Add(new Panel
        {
            Name = "__glowvaScreenLayoutInstalled",
            Size = Size.Empty,
            Visible = false
        });

        form.AutoScaleMode = AutoScaleMode.Font;
        form.AutoScroll = false;
        form.HorizontalScroll.Enabled = false;
        form.VerticalScroll.Enabled = false;
        form.MinimumSize = new Size(
            Math.Max(form.MinimumSize.Width, 1000),
            Math.Max(form.MinimumSize.Height, 650));

        SetFontRecursive(form, form.Font);
        ConfigureTree(form);

        form.Resize += (_, _) => Reflow(form);
        form.Layout += (_, _) => Reflow(form);
        Reflow(form);
    }

    private static void SetFontRecursive(Control control, Font font)
    {
        foreach (Control child in control.Controls)
        {
            if (child is not Label && child is not Button)
                child.Font = font;

            SetFontRecursive(child, font);
        }
    }

    private static void ConfigureTree(Control root)
    {
        foreach (Control child in root.Controls)
        {
            ConfigureControl(child);
            ConfigureTree(child);
        }
    }

    private static void ConfigureControl(Control control)
    {
        if (control is DataGridView grid)
        {
            grid.ScrollBars = ScrollBars.Both;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.AllowUserToResizeRows = true;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 32);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.MinimumSize = new Size(260, MinGridHeight);
        }
        else if (control is TextBox textBox && textBox.Multiline)
        {
            textBox.ScrollBars = ScrollBars.Vertical;
        }
        else if (control is SplitContainer split)
        {
            split.FixedPanel = FixedPanel.None;
            split.Panel1MinSize = MinSplitPanel;
            split.Panel2MinSize = MinSplitPanel;
            split.SplitterWidth = Math.Max(split.SplitterWidth, 6);
            split.SplitterMoved += (_, _) => ClampSplitter(split);
        }
        else if (control is Panel panel && !panel.Name.StartsWith("__glowvaContextSidebar", StringComparison.Ordinal))
        {
            if (panel.Dock == DockStyle.None)
                panel.AutoScroll = true;
        }
    }

    private static void Reflow(Form form)
    {
        if (form.IsDisposed)
            return;

        foreach (Control control in EnumerateControls(form))
        {
            if (control is SplitContainer split)
                ClampSplitter(split);

            if (control is DataGridView grid)
            {
                grid.ScrollBars = ScrollBars.Both;
                if (grid.Height > 0 && grid.Height < MinGridHeight && grid.Dock == DockStyle.None)
                    grid.Height = MinGridHeight;
            }
        }
    }

    private static void ClampSplitter(SplitContainer split)
    {
        if (split.Orientation == Orientation.Horizontal)
        {
            var min = split.Panel1MinSize;
            var max = Math.Max(min, split.Height - split.Panel2MinSize - split.SplitterWidth);
            if (max >= min)
                split.SplitterDistance = Math.Clamp(split.SplitterDistance, min, max);
        }
        else
        {
            var min = split.Panel1MinSize;
            var max = Math.Max(min, split.Width - split.Panel2MinSize - split.SplitterWidth);
            if (max >= min)
                split.SplitterDistance = Math.Clamp(split.SplitterDistance, min, max);
        }
    }

    private static IEnumerable<Control> EnumerateControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in EnumerateControls(child))
                yield return nested;
        }
    }
}
