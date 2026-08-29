using GlowvaERP.Data;
using GlowvaERP.Helpers;

namespace GlowvaERP.Forms;

/// <summary>
/// عرض تفاصيل فاتورة كاملة (مبيعات أو مشتريات).
/// </summary>
public sealed class InvoiceDetailForm : Form
{
    private readonly long _invoiceId;
    private readonly long _invoiceNumber;
    private readonly bool _isSales;
    private readonly DataGridView _grid = new();
    private readonly Label _headerLabel = new();
    private readonly Label _partyLabel = new();
    private readonly Label _dateLabel = new();
    private readonly Label _totalLabel = new();
    private readonly Label _discountLabel = new();
    private readonly Label _netLabel = new();
    private readonly Label _paymentLabel = new();
    private readonly Label _notesLabel = new();

    public InvoiceDetailForm(long invoiceId, long invoiceNumber, bool isSales)
    {
        _invoiceId = invoiceId;
        _invoiceNumber = invoiceNumber;
        _isSales = isSales;

        Text = $"{(isSales ? "فاتورة مبيعات" : "فاتورة مشتريات")} رقم {invoiceNumber}";
        Size = new Size(900, 640);
        MinimumSize = new Size(800, 540);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;

        BuildUi();
        LoadData();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape) { Close(); return true; }
        if (keyData == Keys.F9) { Print(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        var headerBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = Color.FromArgb(27, 94, 32),
        };
        _headerLabel.Dock = DockStyle.Fill;
        _headerLabel.Text = Text;
        _headerLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        _headerLabel.ForeColor = Color.White;
        _headerLabel.TextAlign = ContentAlignment.MiddleCenter;
        headerBar.Controls.Add(_headerLabel);

        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(12, 6, 12, 6),
            BackColor = Color.FromArgb(248, 248, 248),
            RightToLeft = RightToLeft.Yes
        };
        for (var i = 0; i < 6; i++)
            info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.66f));

        info.Controls.Add(FieldLabel("العميل / المورد"), 0, 0);
        info.Controls.Add(_partyLabel, 1, 0);
        info.Controls.Add(FieldLabel("التاريخ"), 2, 0);
        info.Controls.Add(_dateLabel, 3, 0);
        info.Controls.Add(FieldLabel("طريقة الدفع"), 4, 0);
        info.Controls.Add(_paymentLabel, 5, 0);
        info.Controls.Add(FieldLabel("ملاحظات"), 0, 1);
        info.Controls.Add(_notesLabel, 1, 1);
        info.SetColumnSpan(_notesLabel, 5);

        foreach (var label in new[] { _partyLabel, _dateLabel, _paymentLabel, _notesLabel })
        {
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleRight;
            label.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        }

        ScrollableLayout.ConfigureGrid(_grid, 36);
        _grid.Dock = DockStyle.Fill;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233);
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.RightToLeft = RightToLeft.Yes;

        _grid.Columns.AddRange(
            GridColumn("م", "#", 50, center: true),
            GridColumn("اسم الصنف", "name", 300, fill: true),
            GridColumn("الكمية", "qty", 100, right: true),
            GridColumn("السعر", "price", 120, right: true),
            GridColumn("الإجمالي", "total", 130, right: true));

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            ColumnCount = 5,
            RowCount = 1,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Color.FromArgb(248, 248, 248),
            RightToLeft = RightToLeft.Yes
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        foreach (var label in new[] { _totalLabel, _discountLabel, _netLabel })
        {
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(27, 94, 32);
        }

        var printButton = MakeButton("🖨  طباعة  F9", Color.FromArgb(52, 152, 219));
        var closeButton = MakeButton("إغلاق", Color.FromArgb(120, 120, 120));
        printButton.Click += (_, _) => Print();
        closeButton.Click += (_, _) => Close();

        footer.Controls.Add(new Panel(), 0, 0);
        footer.Controls.Add(_totalLabel, 1, 0);
        footer.Controls.Add(_discountLabel, 2, 0);
        footer.Controls.Add(_netLabel, 3, 0);
        footer.Controls.Add(closeButton, 4, 0);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(info);
        Controls.Add(headerBar);
    }

    private void LoadData()
    {
        var table = _isSales ? "orders" : "purchases";
        var itemTable = _isSales ? "order_items" : "purchase_items";
        var fkColumn = _isSales ? "order_id" : "purchase_id";
        var partyTable = _isSales ? "customers" : "suppliers";
        var partyFk = _isSales ? "customer_id" : "supplier_id";
        var dateColumn = _isSales ? "order_date" : "purchase_date";

        using var connection = Database.OpenConnection();
        using var headerCommand = connection.CreateCommand();
        headerCommand.CommandText = $"""
            SELECT COALESCE(p.name,''), o.{dateColumn}, o.payment_status, COALESCE(o.notes,''), o.discount
            FROM {table} o
            LEFT JOIN {partyTable} p ON p.id = o.{partyFk}
            WHERE o.id=$id;
            """;
        headerCommand.Parameters.AddWithValue("$id", _invoiceId);
        using var headerReader = headerCommand.ExecuteReader();
        if (headerReader.Read())
        {
            _partyLabel.Text = headerReader.GetString(0);
            _dateLabel.Text = headerReader.GetString(1);
            _paymentLabel.Text = headerReader.GetString(2);
            _notesLabel.Text = headerReader.GetString(3);
        }

        using var itemCommand = connection.CreateCommand();
        itemCommand.CommandText = $"""
            SELECT p.name, i.quantity, i.unit_price, i.quantity * i.unit_price - COALESCE(i.discount,0)
            FROM {itemTable} i
            JOIN products p ON p.id=i.product_id
            WHERE i.{fkColumn}=$id
            ORDER BY i.id;
            """;
        itemCommand.Parameters.AddWithValue("$id", _invoiceId);

        decimal subtotal = 0m;
        var rowNumber = 1;
        using var itemReader = itemCommand.ExecuteReader();
        while (itemReader.Read())
        {
            var total = itemReader.IsDBNull(3) ? 0m : Convert.ToDecimal(itemReader.GetValue(3));
            subtotal += total;
            _grid.Rows.Add(
                rowNumber++,
                itemReader.IsDBNull(0) ? "" : itemReader.GetString(0),
                itemReader.IsDBNull(1) ? "0" : Convert.ToDecimal(itemReader.GetValue(1)).ToString("N2"),
                itemReader.IsDBNull(2) ? "0" : Convert.ToDecimal(itemReader.GetValue(2)).ToString("N2"),
                total.ToString("N2"));
        }

        using var discountCommand = connection.CreateCommand();
        discountCommand.CommandText = $"SELECT discount FROM {table} WHERE id=$id;";
        discountCommand.Parameters.AddWithValue("$id", _invoiceId);
        var discount = Convert.ToDecimal(discountCommand.ExecuteScalar() ?? 0m);
        var net = Math.Max(0m, subtotal - discount);

        _totalLabel.Text = $"الإجمالي: {subtotal:N2}";
        _discountLabel.Text = $"الخصم: {discount:N2}";
        _netLabel.Text = $"الصافي: {net:N2}";
    }

    private void Print()
    {
        MessageBox.Show(this,
            $"طباعة الفاتورة رقم {_invoiceNumber}\n\nميزة الطباعة الحرارية وA4 ستُربط لاحقًا بتقارير الطباعة.",
            "طباعة", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static DataGridViewTextBoxColumn GridColumn(string header, string name, int width,
        bool fill = false, bool right = false, bool center = false) => new()
    {
        HeaderText = header,
        Name = name,
        Width = width,
        SortMode = DataGridViewColumnSortMode.NotSortable,
        AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = center ? DataGridViewContentAlignment.MiddleCenter
                : right ? DataGridViewContentAlignment.MiddleRight
                : DataGridViewContentAlignment.MiddleRight
        }
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 9),
        ForeColor = Color.FromArgb(100, 100, 100)
    };

    private static Button MakeButton(string text, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0 }
    };
}
