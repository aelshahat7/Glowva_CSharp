using GlowvaERP.Data;
using GlowvaERP.Services;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace GlowvaERP.Forms;

public sealed class AccountsForm : Form
{
    private readonly ComboBox _type = new();
    private readonly ComboBox _party = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly DataGridView _grid = new();
    private readonly Label _balance = new();
    private readonly Label _debitTotal = new();
    private readonly Label _creditTotal = new();
    private readonly LegacyAccountRepairService _legacyRepair = new();
    private bool _loadingParty;

    public AccountsForm()
    {
        Text = "الحسابات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        KeyPreview = true;
        KeyDown += AccountsForm_KeyDown;
        BuildUi();
        try { _legacyRepair.EnsureRebuilt(); } catch (Exception ex) { MessageBox.Show(this, $"تعذر تجهيز الحركات التاريخية للحسابات:\n{ex.Message}", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        LoadParties();
    }

    private void BuildUi()
    {
        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            ColumnCount = 8,
            RowCount = 2,
            Padding = new Padding(12),
            RightToLeft = RightToLeft.Yes
        };

        for (int i = 0; i < 8; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));

        filters.Controls.Add(MakeLabel("نوع الحساب"), 0, 0);
        filters.Controls.Add(MakeLabel("الحساب"), 2, 0);
        filters.Controls.Add(MakeLabel("من التاريخ"), 4, 0);
        filters.Controls.Add(MakeLabel("إلى التاريخ"), 6, 0);

        _type.Dock = DockStyle.Fill;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.RightToLeft = RightToLeft.Yes;
        _type.Items.AddRange(new object[] { "العملاء", "الموردين" });
        _type.SelectedIndex = 0;
        _type.SelectedIndexChanged += (_, _) => LoadParties();

        _party.Dock = DockStyle.Fill;
        _party.DropDownStyle = ComboBoxStyle.DropDownList;
        _party.RightToLeft = RightToLeft.Yes;
        _party.SelectedIndexChanged += (_, _) => { if (!_loadingParty) LoadStatement(); };

        ConfigureDate(_from, DateTime.Today.AddYears(-2));
        ConfigureDate(_to, DateTime.Today);

        filters.Controls.Add(_type, 1, 0);
        filters.Controls.Add(_party, 3, 0);
        filters.Controls.Add(_from, 5, 0);
        filters.Controls.Add(_to, 7, 0);

        var search = MakeButton("بحث", Color.FromArgb(52, 152, 219));
        search.Click += (_, _) => LoadStatement();
        filters.Controls.Add(MakeLabel(""), 0, 1);
        filters.Controls.Add(search, 1, 1);

        var refresh = MakeButton("تحديث الحسابات", Color.FromArgb(120, 120, 120));
        refresh.Click += (_, _) => { LoadParties(); LoadStatement(); };
        filters.Controls.Add(refresh, 2, 1);

        var statement = MakeButton("كشف حساب جديد", Color.FromArgb(39, 174, 96));
        statement.Click += (_, _) => LoadStatement();
        filters.Controls.Add(statement, 3, 1);

        Controls.Add(_grid);
        Controls.Add(BuildSummary());
        Controls.Add(filters);
        ConfigureGrid();
    }

    private Panel BuildSummary()
    {
        var summary = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 92,
            Padding = new Padding(12),
            BackColor = Color.Gainsboro,
            RightToLeft = RightToLeft.Yes
        };

        _balance.AutoSize = false;
        _balance.Width = 330;
        _balance.Dock = DockStyle.Right;
        _balance.TextAlign = ContentAlignment.MiddleRight;
        _balance.Font = new Font("Segoe UI", 16, FontStyle.Bold);

        _debitTotal.AutoSize = false;
        _debitTotal.Width = 230;
        _debitTotal.Dock = DockStyle.Right;
        _debitTotal.TextAlign = ContentAlignment.MiddleRight;
        _debitTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        _creditTotal.AutoSize = false;
        _creditTotal.Width = 230;
        _creditTotal.Dock = DockStyle.Right;
        _creditTotal.TextAlign = ContentAlignment.MiddleRight;
        _creditTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        summary.Controls.Add(_balance);
        summary.Controls.Add(_debitTotal);
        summary.Controls.Add(_creditTotal);
        return summary;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.RowTemplate.Height = 40;

        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "date", HeaderText = "التاريخ", FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "reference", HeaderText = "المرجع", FillWeight = 15 },
            new DataGridViewTextBoxColumn { Name = "notes", HeaderText = "البيان", FillWeight = 34 },
            new DataGridViewTextBoxColumn { Name = "debit", HeaderText = "مدين", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "credit", HeaderText = "دائن", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "balance", HeaderText = "الرصيد", FillWeight = 14 }
        );
    }

    private void LoadParties()
    {
        _loadingParty = true;
        try
        {
            var items = new List<PartyChoice> { new(0, "اختر الحساب") };
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = _type.SelectedIndex == 0
                ? "SELECT id, name FROM customers WHERE is_active = 1 ORDER BY name;"
                : "SELECT id, name FROM suppliers WHERE is_active = 1 ORDER BY name;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
                items.Add(new PartyChoice(reader.GetInt64(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));

            _party.DataSource = items;
            _party.DisplayMember = nameof(PartyChoice.Name);
            _party.ValueMember = nameof(PartyChoice.Id);
            _party.SelectedIndex = items.Count > 1 ? 1 : 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل الحسابات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _loadingParty = false;
        }
    }

    private void LoadStatement()
    {
        var partyId = Convert.ToInt64(_party.SelectedValue ?? 0);
        if (partyId <= 0)
        {
            ClearStatement();
            return;
        }

        try
        {
            using var connection = Database.OpenConnection();
            var accountType = _type.SelectedIndex == 0 ? "customer" : "supplier";
            var openingSql = accountType == "customer"
                ? "SELECT COALESCE(opening_balance,0) FROM customers WHERE id=$id"
                : "SELECT COALESCE(opening_balance,0) FROM suppliers WHERE id=$id";

            decimal opening;
            using (var openingCommand = connection.CreateCommand())
            {
                openingCommand.CommandText = openingSql;
                openingCommand.Parameters.AddWithValue("$id", partyId);
                opening = Convert.ToDecimal(openingCommand.ExecuteScalar() ?? 0m);
            }

            using var movement = connection.CreateCommand();
            movement.CommandText = """
                SELECT transaction_date, reference_type, reference_id, debit, credit, notes
                FROM account_transactions
                WHERE account_type=$type AND party_id=$party
                ORDER BY id;
                """;
            movement.Parameters.AddWithValue("$type", accountType);
            movement.Parameters.AddWithValue("$party", partyId);

            var from = _from.Value.Date;
            var to = _to.Value.Date;
            if (from > to)
                (from, to) = (to, from);

            _grid.Rows.Clear();
            decimal debitTotal = 0m;
            decimal creditTotal = 0m;
            decimal running = opening;

            using var reader = movement.ExecuteReader();
            while (reader.Read())
            {
                var rawDate = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                if (!TryParseLegacyDate(rawDate, out var movementDate))
                    continue;

                if (movementDate.Date > to)
                    continue;

                var referenceType = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var referenceId = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2));
                var debit = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3));
                var credit = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));
                var notes = reader.IsDBNull(5) ? "" : reader.GetString(5);

                var isAfterFrom = movementDate.Date >= from;
                if (accountType == "customer")
                    running += debit - credit;
                else
                    running += credit - debit;

                if (!isAfterFrom)
                    continue;

                debitTotal += debit;
                creditTotal += credit;

                _grid.Rows.Add(
                    movementDate.ToString("yyyy-MM-dd"),
                    FormatReference(referenceType, referenceId),
                    notes,
                    debit.ToString("N2"),
                    credit.ToString("N2"),
                    running.ToString("N2"));
            }

            _balance.Text = $"الرصيد: {running:N2}";
            _debitTotal.Text = $"إجمالي المدين: {debitTotal:N2}";
            _creditTotal.Text = $"إجمالي الدائن: {creditTotal:N2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل كشف الحساب:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool TryParseLegacyDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Contains('T') && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date))
            return true;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
            && serial > 20000 && serial < 80000)
        {
            try
            {
                date = DateTime.FromOADate(serial);
                return true;
            }
            catch { }
        }

        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy"
        };

        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    }

    private void ClearStatement()
    {
        _grid.Rows.Clear();
        _balance.Text = "الرصيد: 0.00";
        _debitTotal.Text = "إجمالي المدين: 0.00";
        _creditTotal.Text = "إجمالي الدائن: 0.00";
    }

    private void AccountsForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            LoadParties();
            LoadStatement();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.SuppressKeyPress = true;
        }
    }

    private static void ConfigureDate(DateTimePicker picker, DateTime value)
    {
        picker.Dock = DockStyle.Fill;
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "yyyy-MM-dd";
        picker.Value = value;
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        RightToLeft = RightToLeft.Yes
    };

    private static Button MakeButton(string text, Color color) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };

    private static string FormatReference(string type, long id) => string.IsNullOrWhiteSpace(type)
        ? ""
        : type switch
        {
            "order" => $"فاتورة بيع #{id}",
            "purchase" => $"فاتورة شراء #{id}",
            "sales_return" => $"مرتجع بيع #{id}",
            "purchase_return" => $"مرتجع شراء #{id}",
            "payment" => $"سداد #{id}",
            _ => $"{type} #{id}"
        };

    private sealed record PartyChoice(long Id, string Name);
}
