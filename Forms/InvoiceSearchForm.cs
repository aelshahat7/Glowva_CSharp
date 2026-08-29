using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Helpers;

namespace GlowvaERP.Forms;

public sealed class InvoiceSearchForm : Form
{
    private readonly bool _salesMode;
    private readonly TextBox _invoice = new();
    private readonly ComboBox _party = new();
    private readonly ComboBox _payment = new();
    private readonly TextBox _product = new();
    private readonly CheckBox _allDates = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly DataGridView _grid = new();

    public InvoiceSearchForm(bool salesMode)
    {
        _salesMode = salesMode;
        Text = salesMode ? "بحث فواتير المبيعات" : "بحث فواتير المشتريات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 700);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        KeyPreview = true;
        ScrollableLayout.PrepareForm(this, 1100, 650);
        BuildUi();
        LoadParties();
        Search();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { Search(); return true; }
        if (keyData == Keys.Enter && _grid.CurrentRow is not null) { RecallSelectedInvoice(); return true; }
        if (keyData == Keys.Escape) { Close(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 145,
            Padding = new Padding(12),
            ColumnCount = 4,
            RowCount = 3,
            RightToLeft = RightToLeft.Yes
        };
        for (int i = 0; i < 4; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (int i = 0; i < 3; i++)
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

        _invoice.PlaceholderText = "رقم الفاتورة";
        _invoice.Dock = DockStyle.Fill;
        _invoice.TextAlign = HorizontalAlignment.Right;
        _invoice.RightToLeft = RightToLeft.Yes;

        _party.DropDownStyle = ComboBoxStyle.DropDownList;
        _party.Dock = DockStyle.Fill;
        _party.RightToLeft = RightToLeft.Yes;

        _payment.DropDownStyle = ComboBoxStyle.DropDownList;
        _payment.Dock = DockStyle.Fill;
        _payment.RightToLeft = RightToLeft.Yes;
        _payment.Items.AddRange(new object[]
        {
            "كل طرق الدفع",
            "كاش",
            "فيزا",
            "آجل",
            "محفظة إلكترونية",
            "تحويل بنكي"
        });
        _payment.SelectedIndex = 0;

        _product.PlaceholderText = "اسم / كود / باركود الصنف";
        _product.Dock = DockStyle.Fill;
        _product.TextAlign = HorizontalAlignment.Right;
        _product.RightToLeft = RightToLeft.Yes;

        _allDates.Text = "كل التواريخ";
        _allDates.Checked = true;
        _allDates.Dock = DockStyle.Fill;
        _allDates.CheckedChanged += (_, _) =>
        {
            _from.Enabled = !_allDates.Checked;
            _to.Enabled = !_allDates.Checked;
        };

        _from.Format = DateTimePickerFormat.Short;
        _from.Value = DateTime.Today.AddMonths(-1);
        _from.Dock = DockStyle.Fill;
        _from.Enabled = false;

        _to.Format = DateTimePickerFormat.Short;
        _to.Value = DateTime.Today;
        _to.Dock = DockStyle.Fill;
        _to.Enabled = false;

        var search = new Button
        {
            Text = "بحث",
            Dock = DockStyle.Fill,
            Height = 38,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        search.Click += (_, _) => Search();

        filters.Controls.Add(MakeLabel("رقم الفاتورة"), 0, 0);
        filters.Controls.Add(_invoice, 1, 0);
        filters.Controls.Add(MakeLabel(_salesMode ? "العميل" : "المورد"), 2, 0);
        filters.Controls.Add(_party, 3, 0);

        filters.Controls.Add(MakeLabel("طريقة الدفع"), 0, 1);
        filters.Controls.Add(_payment, 1, 1);
        filters.Controls.Add(MakeLabel("الصنف داخل الفاتورة"), 2, 1);
        filters.Controls.Add(_product, 3, 1);

        filters.Controls.Add(_allDates, 0, 2);
        filters.Controls.Add(_from, 1, 2);
        filters.Controls.Add(_to, 2, 2);
        filters.Controls.Add(search, 3, 2);

        ConfigureGrid();
        Controls.Add(_grid);
        Controls.Add(filters);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        RightToLeft = RightToLeft.Yes
    };

    private void ConfigureGrid()
    {
        ScrollableLayout.ConfigureGrid(_grid, 40);
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        AddColumn("رقم الفاتورة", "InvoiceNumber", 130);
        AddColumn(_salesMode ? "العميل" : "المورد", "PartyName", 260);
        AddColumn("التاريخ", "DateText", 140);
        AddColumn("الدفع", "PaymentStatus", 160);
        AddColumn("الإجمالي", "Total", 140, "N2");
        AddColumn("الأصناف", "ItemsSummary", 500);

        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                RecallSelectedInvoice();
        };

        var menu = new ContextMenuStrip
        {
            RightToLeft = RightToLeft.Yes
        };
        var recall = new ToolStripMenuItem("استدعاء الفاتورة");
        recall.Click += (_, _) => RecallSelectedInvoice();
        var details = new ToolStripMenuItem("عرض تفاصيل الفاتورة");
        details.Click += (_, _) => ShowSelectedDetails();
        menu.Items.Add(recall);
        menu.Items.Add(details);
        _grid.ContextMenuStrip = menu;
    }

    private void AddColumn(string header, string property, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            AutoSizeMode = property == "ItemsSummary" ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = format ?? string.Empty
            }
        };
        _grid.Columns.Add(column);
    }

    private void LoadParties()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = _salesMode
            ? "SELECT id,name FROM customers WHERE is_active=1 ORDER BY name;"
            : "SELECT id,name FROM suppliers WHERE is_active=1 ORDER BY name;";

        var list = new List<PartyChoice>
        {
            new(0, _salesMode ? "كل العملاء" : "كل الموردين")
        };

        using var reader = command.ExecuteReader();
        while (reader.Read())
            list.Add(new PartyChoice(reader.GetInt64(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));

        _party.DataSource = list;
        _party.DisplayMember = nameof(PartyChoice.Name);
        _party.ValueMember = nameof(PartyChoice.Id);
    }

    private void Search()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();

            string table = _salesMode ? "orders" : "purchases";
            string itemTable = _salesMode ? "order_items" : "purchase_items";
            string itemFk = _salesMode ? "order_id" : "purchase_id";
            string dateCol = _salesMode ? "order_date" : "purchase_date";
            string partyCol = _salesMode ? "customer_id" : "supplier_id";
            string partyTable = _salesMode ? "customers" : "suppliers";
            string itemTotal = _salesMode ? "oi.quantity * oi.unit_price" : "pi.quantity * pi.unit_price";

            command.CommandText = $@"
SELECT x.id,
       x.invoice_number,
       x.{dateCol},
       COALESCE(pt.name,'') party_name,
       COALESCE(x.payment_status,'') payment_status,
       COALESCE((SELECT SUM({itemTotal})
                 FROM {itemTable} oi
                 JOIN products p ON p.id=oi.product_id
                 WHERE oi.{itemFk}=x.id),0) - COALESCE(x.discount,0) total,
       COALESCE((SELECT GROUP_CONCAT(p.name, '، ')
                 FROM {itemTable} oi
                 JOIN products p ON p.id=oi.product_id
                 WHERE oi.{itemFk}=x.id),'') items_summary
FROM {table} x
LEFT JOIN {partyTable} pt ON pt.id=x.{partyCol}
WHERE 1=1";

            if (long.TryParse(_invoice.Text.Trim(), out var invoice))
            {
                command.CommandText += " AND x.invoice_number=$invoice";
                command.Parameters.AddWithValue("$invoice", invoice);
            }

            long partyId = Convert.ToInt64(_party.SelectedValue ?? 0);
            if (partyId > 0)
            {
                command.CommandText += $" AND x.{partyCol}=$party";
                command.Parameters.AddWithValue("$party", partyId);
            }

            if (_payment.SelectedIndex > 0)
            {
                command.CommandText += " AND x.payment_status=$payment";
                command.Parameters.AddWithValue("$payment", _payment.Text);
            }

            if (!string.IsNullOrWhiteSpace(_product.Text))
            {
                command.CommandText += $" AND EXISTS (SELECT 1 FROM {itemTable} qi JOIN products qp ON qp.id=qi.product_id WHERE qi.{itemFk}=x.id AND (qp.name LIKE $product OR qp.code LIKE $product OR qp.barcode LIKE $product))";
                command.Parameters.AddWithValue("$product", $"%{_product.Text.Trim()}%");
            }

            if (!_allDates.Checked)
            {
                command.CommandText += $" AND date(x.{dateCol}) BETWEEN date($from) AND date($to)";
                command.Parameters.AddWithValue("$from", _from.Value.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("$to", _to.Value.ToString("yyyy-MM-dd"));
            }

            command.CommandText += $" ORDER BY x.{dateCol} DESC, x.id DESC LIMIT 1000;";

            var rows = new List<InvoiceRow>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new InvoiceRow(
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    reader.IsDBNull(3) ? "" : reader.GetString(3),
                    reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Convert.ToDecimal(reader.GetValue(5)),
                    reader.IsDBNull(6) ? "" : reader.GetString(6)));
            }

            _grid.DataSource = rows;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر البحث:\n{ex.Message}", "بحث الفواتير", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RecallSelectedInvoice()
    {
        if (_grid.CurrentRow?.DataBoundItem is not InvoiceRow row)
        {
            MessageBox.Show(this, "اختر فاتورة أولًا.", "استدعاء الفاتورة", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new InvoiceDetailsDialog(_salesMode, row.Id);
        dialog.ShowDialog(this);
    }

    private void ShowSelectedDetails() => RecallSelectedInvoice();

    private sealed record PartyChoice(long Id, string Name);
    private sealed record InvoiceRow(long Id, long InvoiceNumber, string DateText, string PartyName, string PaymentStatus, decimal Total, string ItemsSummary);
}
