using GlowvaERP.Data;
using System.Globalization;

namespace GlowvaERP.Forms;

public sealed class CustomerAccountsReportForm : Form
{
    private readonly ComboBox _customer = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly DataGridView _grid = new();
    private readonly Label _debitTotal = new();
    private readonly Label _creditTotal = new();
    private readonly Label _balance = new();
    private bool _loadingCustomers;

    public CustomerAccountsReportForm()
    {
        Text = "كشف حساب العملاء";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        KeyPreview = true;
        BuildUi();
        LoadCustomers();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { LoadStatement(); return true; }
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
            Padding = new Padding(10),
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));

        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 2,
            Padding = new Padding(4),
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(248, 248, 248)
        };
        for (var i = 0; i < 8; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        filters.Controls.Add(LabelOf("التقرير"), 0, 0);
        filters.Controls.Add(new Label
        {
            Text = "كشف حساب العملاء",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        }, 1, 0);

        filters.Controls.Add(LabelOf("العميل"), 2, 0);
        _customer.Dock = DockStyle.Fill;
        _customer.DropDownStyle = ComboBoxStyle.DropDownList;
        _customer.RightToLeft = RightToLeft.Yes;
        filters.Controls.Add(_customer, 3, 0);

        filters.Controls.Add(LabelOf("من"), 4, 0);
        ConfigureDate(_from, DateTime.Today.AddMonths(-1));
        filters.Controls.Add(_from, 5, 0);
        filters.Controls.Add(LabelOf("إلى"), 6, 0);
        ConfigureDate(_to, DateTime.Today);
        filters.Controls.Add(_to, 7, 0);

        var search = ButtonOf("تشغيل", Color.FromArgb(33, 150, 243));
        search.Click += (_, _) => LoadStatement();
        filters.Controls.Add(search, 0, 1);

        var close = ButtonOf("إغلاق", Color.FromArgb(90, 90, 90));
        close.Click += (_, _) => Close();
        filters.Controls.Add(close, 1, 1);

        root.Controls.Add(filters, 0, 0);
        ConfigureGrid();
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(BuildSummary(), 0, 2);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.RowTemplate.Height = 32;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 34;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 121, 107);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "date", HeaderText = "التاريخ", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "customer", HeaderText = "العميل", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "reference", HeaderText = "المرجع", FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "notes", HeaderText = "البيان", FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "debit", HeaderText = "مدين", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "credit", HeaderText = "دائن", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "balance", HeaderText = "الرصيد", FillWeight = 12 });
    }

    private Panel BuildSummary()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Gainsboro, Padding = new Padding(10), RightToLeft = RightToLeft.Yes };
        _balance.Dock = DockStyle.Right;
        _balance.Width = 330;
        _balance.TextAlign = ContentAlignment.MiddleRight;
        _balance.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _debitTotal.Dock = DockStyle.Right;
        _debitTotal.Width = 260;
        _debitTotal.TextAlign = ContentAlignment.MiddleRight;
        _debitTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _creditTotal.Dock = DockStyle.Right;
        _creditTotal.Width = 260;
        _creditTotal.TextAlign = ContentAlignment.MiddleRight;
        _creditTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        panel.Controls.Add(_balance);
        panel.Controls.Add(_debitTotal);
        panel.Controls.Add(_creditTotal);
        return panel;
    }

    private void LoadCustomers()
    {
        _loadingCustomers = true;
        try
        {
            var items = new List<CustomerChoice> { new(0, "كل العملاء") };
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name FROM customers WHERE is_active = 1 ORDER BY name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                items.Add(new CustomerChoice(reader.GetInt64(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
            _customer.DataSource = items;
            _customer.DisplayMember = nameof(CustomerChoice.Name);
            _customer.ValueMember = nameof(CustomerChoice.Id);
            _customer.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل العملاء:\n{ex.Message}", "كشف حساب العملاء", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _loadingCustomers = false; }
    }

    private void LoadStatement()
    {
        try
        {
            var from = _from.Value.Date;
            var to = _to.Value.Date;
            if (from > to) (from, to) = (to, from);
            var customerId = Convert.ToInt64(_customer.SelectedValue ?? 0);

            using var connection = Database.OpenConnection();
            _grid.Rows.Clear();
            decimal debitTotal = 0m;
            decimal creditTotal = 0m;

            var customers = new Dictionary<long, (string Name, decimal Opening)>();
            using (var customerCommand = connection.CreateCommand())
            {
                customerCommand.CommandText = customerId > 0
                    ? "SELECT id, name, COALESCE(opening_balance,0) FROM customers WHERE id=$id;"
                    : "SELECT id, name, COALESCE(opening_balance,0) FROM customers WHERE is_active=1 ORDER BY name;";
                if (customerId > 0) customerCommand.Parameters.AddWithValue("$id", customerId);
                using var reader = customerCommand.ExecuteReader();
                while (reader.Read())
                    customers[reader.GetInt64(0)] = (reader.IsDBNull(1) ? string.Empty : reader.GetString(1), Convert.ToDecimal(reader.GetValue(2)));
            }

            foreach (var customer in customers)
            {
                decimal opening = customer.Value.Opening;
                using (var openingCommand = connection.CreateCommand())
                {
                    openingCommand.CommandText = "SELECT COALESCE(SUM(debit-credit),0) FROM account_transactions WHERE account_type='customer' AND party_id=$party AND date(transaction_date) < date($from);";
                    openingCommand.Parameters.AddWithValue("$party", customer.Key);
                    openingCommand.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
                    opening += Convert.ToDecimal(openingCommand.ExecuteScalar() ?? 0m);
                }

                var running = opening;
                if (customerId > 0)
                {
                    _grid.Rows.Add(from.ToString("yyyy/MM/dd"), customer.Value.Name, "رصيد أول المدة", "الرصيد المرحل", "0.00", "0.00", running.ToString("N2"));
                }

                using var command = connection.CreateCommand();
                command.CommandText = """
                    SELECT at.transaction_date, at.reference_type, at.reference_id,
                           COALESCE(at.debit,0), COALESCE(at.credit,0), COALESCE(at.notes,''),
                           COALESCE(o.invoice_number, sr.return_number, 0)
                    FROM account_transactions at
                    LEFT JOIN orders o ON at.reference_type='order' AND at.reference_id=o.id
                    LEFT JOIN sales_returns sr ON at.reference_type='sales_return' AND at.reference_id=sr.id
                    WHERE at.account_type='customer' AND at.party_id=$party
                      AND date(at.transaction_date) BETWEEN date($from) AND date($to)
                    ORDER BY at.transaction_date, at.id;
                    """;
                command.Parameters.AddWithValue("$party", customer.Key);
                command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var dateText = reader.IsDBNull(0) ? string.Empty : reader.GetValue(0)?.ToString() ?? string.Empty;
                    var date = ParseDate(dateText);
                    var debit = Convert.ToDecimal(reader.GetValue(3));
                    var credit = Convert.ToDecimal(reader.GetValue(4));
                    running += debit - credit;
                    debitTotal += debit;
                    creditTotal += credit;
                    var referenceType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                    var referenceId = reader.IsDBNull(2) ? 0L : Convert.ToInt64(reader.GetValue(2));
                    var notes = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                    var reference = referenceId > 0 ? $"{referenceType} #{referenceId}" : referenceType;
                    _grid.Rows.Add(date.ToString("yyyy/MM/dd HH:mm"), customer.Value.Name, reference, notes, debit.ToString("N2"), credit.ToString("N2"), running.ToString("N2"));
                }
            }

            var finalBalance = customers.Values.Sum(x => x.Opening);
            if (customerId > 0 && _grid.Rows.Count > 0)
                finalBalance = Convert.ToDecimal(_grid.Rows[_grid.Rows.Count - 1].Cells[6].Value);
            else
                finalBalance += debitTotal - creditTotal;

            _debitTotal.Text = $"إجمالي المدين: {debitTotal:N2}";
            _creditTotal.Text = $"إجمالي الدائن: {creditTotal:N2}";
            _balance.Text = $"الرصيد: {finalBalance:N2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل كشف حساب العملاء:\n{ex.Message}", "كشف حساب العملاء", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static DateTime ParseDate(string value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var result)) return result;
        if (DateTime.TryParse(value, out result)) return result;
        return DateTime.MinValue;
    }

    private static Label LabelOf(string text) => new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(4) };

    private static Button ButtonOf(string text, Color color) => new() { Text = text, Dock = DockStyle.Fill, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(3) };

    private static void ConfigureDate(DateTimePicker picker, DateTime value)
    {
        picker.Dock = DockStyle.Fill;
        picker.Format = DateTimePickerFormat.Short;
        picker.Value = value;
        picker.Margin = new Padding(3);
    }

    private readonly record struct CustomerChoice(long Id, string Name);
}
