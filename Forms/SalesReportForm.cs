using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Helpers;

namespace GlowvaERP.Forms;

public sealed class SalesReportForm : Form
{
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _customer = new();
    private readonly ComboBox _payment = new();
    private readonly TextBox _product = new();
    private readonly DataGridView _grid = new();
    private readonly Label _salesTotal = new();
    private readonly Label _costTotal = new();
    private readonly Label _profitTotal = new();

    public SalesReportForm()
    {
        Text = "تقرير المبيعات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        KeyPreview = true;
        BackColor = Color.White;
        BuildUi();
        LoadCustomers();
        Search();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { Search(); return true; }
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(8),
            BackColor = Color.FromArgb(248, 248, 248),
            RightToLeft = RightToLeft.Yes
        };
        for (var i = 0; i < 6; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666F));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _from.Format = DateTimePickerFormat.Short;
        _from.Value = DateTime.Today.AddMonths(-1);
        _from.Dock = DockStyle.Fill;
        _to.Format = DateTimePickerFormat.Short;
        _to.Value = DateTime.Today;
        _to.Dock = DockStyle.Fill;

        PrepareCombo(_customer);
        PrepareCombo(_payment);
        _payment.Items.AddRange(new object[] { "كل طرق الدفع", "كاش", "فيزا", "آجل", "محفظة إلكترونية", "تحويل بنكي" });
        _payment.SelectedIndex = 0;

        _product.Dock = DockStyle.Fill;
        _product.RightToLeft = RightToLeft.Yes;
        _product.TextAlign = HorizontalAlignment.Right;
        _product.PlaceholderText = "اسم / كود / باركود الصنف";

        var search = CreateButton("بحث", Color.FromArgb(52, 152, 219));
        search.Click += (_, _) => Search();

        filters.Controls.Add(MakeLabel("من التاريخ"), 0, 0);
        filters.Controls.Add(_from, 1, 0);
        filters.Controls.Add(MakeLabel("إلى التاريخ"), 2, 0);
        filters.Controls.Add(_to, 3, 0);
        filters.Controls.Add(MakeLabel("العميل"), 4, 0);
        filters.Controls.Add(_customer, 5, 0);
        filters.Controls.Add(MakeLabel("طريقة الدفع"), 0, 1);
        filters.Controls.Add(_payment, 1, 1);
        filters.Controls.Add(MakeLabel("الصنف"), 2, 1);
        filters.Controls.Add(_product, 3, 1);
        filters.Controls.Add(search, 4, 1);
        var close = CreateButton("إغلاق", Color.FromArgb(100, 100, 100));
        close.Click += (_, _) => Close();
        filters.Controls.Add(close, 5, 1);

        ConfigureGrid();

        var summary = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(6),
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(235, 235, 235)
        };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

        ConfigureSummary(_salesTotal, "إجمالي المبيعات", Color.FromArgb(0, 86, 179));
        ConfigureSummary(_costTotal, "تكلفة المبيعات", Color.FromArgb(90, 90, 90));
        ConfigureSummary(_profitTotal, "صافي الربح", Color.FromArgb(0, 125, 0));
        summary.Controls.Add(_salesTotal, 0, 0);
        summary.Controls.Add(_costTotal, 1, 0);
        summary.Controls.Add(_profitTotal, 2, 0);

        root.Controls.Add(filters, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(summary, 0, 2);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        ScrollableLayout.ConfigureGrid(_grid, 36);
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        AddColumn("رقم الفاتورة", "InvoiceNumber", 10);
        AddColumn("التاريخ", "DateText", 12);
        AddColumn("العميل", "CustomerName", 17);
        AddColumn("طريقة الدفع", "PaymentStatus", 12);
        AddColumn("عدد الأصناف", "ItemsCount", 10);
        AddColumn("الإجمالي", "GrossTotal", 13, "N2");
        AddColumn("الخصم", "Discount", 10, "N2");
        AddColumn("التكلفة", "Cost", 13, "N2");
        AddColumn("الربح", "Profit", 13, "N2");
    }

    private void AddColumn(string header, string property, float weight, string? format = null)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = format ?? string.Empty
            }
        });
    }

    private void Search()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT o.id,
       o.invoice_number,
       o.order_date,
       COALESCE(c.name, 'عميل نقدي') AS customer_name,
       COALESCE(o.payment_status, '') AS payment_status,
       COUNT(oi.id) AS items_count,
       COALESCE(SUM(oi.quantity * oi.unit_price), 0) AS gross_total,
       COALESCE(o.discount, 0) AS discount,
       COALESCE(SUM(oi.quantity * oi.cost_price), 0) AS cost,
       COALESCE(SUM(oi.quantity * oi.unit_price), 0) - COALESCE(o.discount, 0) - COALESCE(SUM(oi.quantity * oi.cost_price), 0) AS profit
FROM orders o
LEFT JOIN customers c ON c.id = o.customer_id
LEFT JOIN order_items oi ON oi.order_id = o.id
WHERE date(o.order_date) BETWEEN date($from) AND date($to)";

            command.Parameters.AddWithValue("$from", _from.Value.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$to", _to.Value.ToString("yyyy-MM-dd"));

            if (Convert.ToInt64(_customer.SelectedValue ?? 0) > 0)
            {
                command.CommandText += " AND o.customer_id=$customer";
                command.Parameters.AddWithValue("$customer", Convert.ToInt64(_customer.SelectedValue));
            }

            if (_payment.SelectedIndex > 0)
            {
                command.CommandText += " AND o.payment_status=$payment";
                command.Parameters.AddWithValue("$payment", _payment.Text);
            }

            if (!string.IsNullOrWhiteSpace(_product.Text))
            {
                command.CommandText += " AND EXISTS (SELECT 1 FROM order_items qoi JOIN products qp ON qp.id=qoi.product_id WHERE qoi.order_id=o.id AND (qp.name LIKE $product OR qp.code LIKE $product OR qp.barcode LIKE $product))";
                command.Parameters.AddWithValue("$product", $"%{_product.Text.Trim()}%");
            }

            command.CommandText += " GROUP BY o.id, o.invoice_number, o.order_date, c.name, o.payment_status, o.discount ORDER BY o.order_date DESC, o.id DESC LIMIT 5000;";

            var rows = new List<SalesReportRow>();
            using var reader = command.ExecuteReader();
            decimal sales = 0, cost = 0, profit = 0;
            while (reader.Read())
            {
                var row = new SalesReportRow(
                    reader.GetInt64(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Convert.ToInt32(reader.GetValue(5)),
                    Convert.ToDecimal(reader.GetValue(6)),
                    Convert.ToDecimal(reader.GetValue(7)),
                    Convert.ToDecimal(reader.GetValue(8)),
                    Convert.ToDecimal(reader.GetValue(9)));
                rows.Add(row);
                sales += row.GrossTotal - row.Discount;
                cost += row.Cost;
                profit += row.Profit;
            }

            _grid.DataSource = rows;
            _salesTotal.Text = $"إجمالي المبيعات: {sales:N2}";
            _costTotal.Text = $"تكلفة المبيعات: {cost:N2}";
            _profitTotal.Text = $"صافي الربح: {profit:N2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل تقرير المبيعات:\n{ex.Message}", "تقرير المبيعات", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadCustomers()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,name FROM customers WHERE is_active=1 ORDER BY name;";
        var list = new List<PartyChoice> { new(0, "كل العملاء") };
        using var reader = command.ExecuteReader();
        while (reader.Read()) list.Add(new PartyChoice(reader.GetInt64(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));
        _customer.DataSource = list;
        _customer.DisplayMember = nameof(PartyChoice.Name);
        _customer.ValueMember = nameof(PartyChoice.Id);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        RightToLeft = RightToLeft.Yes,
        Padding = new Padding(4)
    };

    private static Button CreateButton(string text, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        Margin = new Padding(4)
    };

    private static void PrepareCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.RightToLeft = RightToLeft.Yes;
        combo.Margin = new Padding(3);
    }

    private static void ConfigureSummary(Label label, string title, Color color)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        label.ForeColor = color;
        label.Text = $"{title}: 0.00";
        label.Margin = new Padding(4);
    }

    private sealed record PartyChoice(long Id, string Name);
    private sealed record SalesReportRow(long InvoiceNumber, string DateText, string CustomerName, string PaymentStatus, int ItemsCount, decimal GrossTotal, decimal Discount, decimal Cost, decimal Profit);
}
