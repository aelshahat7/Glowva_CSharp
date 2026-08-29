using System.Drawing;
using System.Windows.Forms;

namespace GlowvaERP.Helpers;

public static class ScrollableLayout
{
    public static Panel CreateViewport(Control content)
    {
        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        content.Dock = DockStyle.Top;
        viewport.Controls.Add(content);
        return viewport;
    }

    public static void ConfigureGrid(DataGridView grid, int rowHeight = 38)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.RowTemplate.Height = rowHeight;
        grid.AllowUserToResizeRows = false;
        grid.ScrollBars = ScrollBars.Both;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 36;
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
    }

    public static void PrepareForm(Form form, int minimumWidth = 1000, int minimumHeight = 650)
    {
        form.MinimumSize = new Size(minimumWidth, minimumHeight);
        form.AutoScaleMode = AutoScaleMode.Font;
        // Scrolling belongs to the work area/grid, not to the whole window.
        // This keeps the contextual sidebar fixed from top to bottom.
        form.AutoScroll = false;
        form.HorizontalScroll.Enabled = false;
        form.VerticalScroll.Enabled = false;
    }
}
