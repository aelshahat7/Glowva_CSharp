using System.Drawing;
using System.Windows.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Shared UI helpers specifically for invoice screens (SalesForm, PurchasesForm).
/// Keeps consistent look between both screens without duplicating code.
/// </summary>
public static class InvoiceUi
{
    public static Label FieldLabel(string text) => new()
    {
        Text      = text,
        AutoSize  = false,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font      = new Font("Segoe UI", 9F),
        ForeColor = Color.FromArgb(50, 50, 50),
        Padding   = new Padding(0, 0, 4, 0),
    };

    public static TableLayoutPanel FieldRow(int columnCount)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columnCount,
            RowCount = 1,
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.White,
            RightToLeft = RightToLeft.Yes,
        };

        for (var i = 0; i < columnCount; i++)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columnCount));

        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        return row;
    }

    public static Label Title(string text, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 18F, FontStyle.Bold),
        ForeColor = color,
        BackColor = Color.White,
        Margin = Padding.Empty,
        Padding = Padding.Empty,
        RightToLeft = RightToLeft.Yes,
    };

    public static Label Metric(string text, bool isCaption = false) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font      = isCaption
            ? new Font("Segoe UI", 7.5F)
            : new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = isCaption
            ? Color.FromArgb(80, 80, 80)
            : Color.FromArgb(20, 100, 20),
        BackColor = isCaption
            ? Color.FromArgb(230, 235, 230)
            : Color.FromArgb(244, 249, 244),
        Padding   = Padding.Empty,
        Margin    = Padding.Empty,
        BorderStyle = BorderStyle.None,
    };

    public static Button ActionButton(string text, Color backColor, int height = 34) => new()
    {
        Text      = text,
        Height    = height,
        BackColor = backColor,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
        Cursor    = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 },
        Margin    = new Padding(2),
    };

    public static Panel SectionDivider() => new()
    {
        Height    = 1,
        Dock      = DockStyle.Top,
        BackColor = Color.FromArgb(200, 200, 200),
        Margin    = Padding.Empty,
    };
}
