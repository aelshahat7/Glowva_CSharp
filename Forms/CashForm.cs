using System.Globalization;
using GlowvaERP.Data;
using Microsoft.Data.Sqlite;

namespace GlowvaERP.Forms;

public sealed class CashForm : Form
{
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _type = new();
    private readonly DataGridView _grid = new();
    private readonly Label _balance = new();
    private readonly Label _inTotal = new();
    private readonly Label _outTotal = new();

    public CashForm()
    {
        Text = "الخزينة";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        KeyPreview = true;
        KeyDown += CashForm_KeyDown;
        BuildUi();
        LoadTransactions();
    }

    private void BuildUi()
    {
        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 150,
            ColumnCount = 8,
            RowCount = 2,
            Padding = new Padding(12),
            RightToLeft = RightToLeft.Yes
        };
        for (int i = 0; i < 8; i++)
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5f));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        filters.Controls.Add(MakeLabel("نوع الحركة"), 0, 0);
        filters.Controls.Add(MakeLabel("من التاريخ"), 2, 0);
        filters.Controls.Add(MakeLabel("إلى التاريخ"), 4, 0);

        _type.Dock = DockStyle.Fill;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.RightToLeft = RightToLeft.Yes;
        _type.Items.AddRange(new object[] { "الكل", "مبيعات", "مشتريات", "صرف", "توريد" });
        _type.SelectedIndex = 0;

        ConfigureDate(_from, DateTime.Today.AddYears(-2));
        ConfigureDate(_to, DateTime.Today);

        filters.Controls.Add(_type, 1, 0);
        filters.Controls.Add(_from, 3, 0);
        filters.Controls.Add(_to, 5, 0);

        var search = MakeButton("بحث", Color.FromArgb(52, 152, 219));
        search.Click += (_, _) => LoadTransactions();
        filters.Controls.Add(search, 1, 1);

        var receipt = MakeButton("توريد", Color.FromArgb(39, 174, 96));
        receipt.Click += (_, _) => AddTransaction(false);
        filters.Controls.Add(receipt, 2, 1);

        var payment = MakeButton("صرف", Color.FromArgb(192, 57, 43));
        payment.Click += (_, _) => AddTransaction(true);
        filters.Controls.Add(payment, 3, 1);

        var refresh = MakeButton("تحديث", Color.FromArgb(120, 120, 120));
        refresh.Click += (_, _) => LoadTransactions();
        filters.Controls.Add(refresh, 4, 1);

        Controls.Add(_grid);
        Controls.Add(BuildSummary());
        Controls.Add(filters);
        ConfigureGrid();
    }

    private Panel BuildSummary()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 92,
            Padding = new Padding(12),
            BackColor = Color.Gainsboro,
            RightToLeft = RightToLeft.Yes
        };

        _balance.Dock = DockStyle.Right;
        _balance.Width = 330;
        _balance.TextAlign = ContentAlignment.MiddleRight;
        _balance.Font = new Font("Segoe UI", 16, FontStyle.Bold);

        _inTotal.Dock = DockStyle.Right;
        _inTotal.Width = 240;
        _inTotal.TextAlign = ContentAlignment.MiddleRight;
        _inTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        _outTotal.Dock = DockStyle.Right;
        _outTotal.Width = 240;
        _outTotal.TextAlign = ContentAlignment.MiddleRight;
        _outTotal.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        panel.Controls.Add(_balance);
        panel.Controls.Add(_inTotal);
        panel.Controls.Add(_outTotal);
        return panel;
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
            new DataGridViewTextBoxColumn { Name = "type", HeaderText = "نوع الحركة", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "reference", HeaderText = "المرجع", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "notes", HeaderText = "البيان", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "in", HeaderText = "وارد", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "out", HeaderText = "منصرف", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "balance", HeaderText = "الرصيد", FillWeight = 14 }
        );
    }

    private void LoadTransactions()
    {
        try
        {
            var from = _from.Value.Date;
            var to = _to.Value.Date;
            if (from > to) (from, to) = (to, from);

            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT transaction_date, transaction_type, reference_type, reference_id, amount_in, amount_out, notes FROM cash_transactions ORDER BY id;";

            _grid.Rows.Clear();
            decimal incoming = 0m;
            decimal outgoing = 0m;
            decimal running = 0m;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var rawDate = reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString();
                if (!TryParseDate(rawDate, out var movementDate))
                    continue;
                if (movementDate.Date > to)
                    continue;

                var movementType = NormalizeType(reader.IsDBNull(1) ? "" : reader.GetString(1));
                if (!MatchesType(movementType))
                {
                    var amountInSkipped = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));
                    var amountOutSkipped = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5));
                    running += amountInSkipped - amountOutSkipped;
                    continue;
                }

                var amountIn = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));
                var amountOut = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5));
                running += amountIn - amountOut;

                if (movementDate.Date < from)
                    continue;

                var referenceType = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var referenceId = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetValue(3));
                var notes = reader.IsDBNull(6) ? "" : reader.GetString(6);

                incoming += amountIn;
                outgoing += amountOut;
                _grid.Rows.Add(
                    movementDate.ToString("yyyy-MM-dd"),
                    movementType,
                    FormatReference(referenceType, referenceId),
                    notes,
                    amountIn.ToString("N2"),
                    amountOut.ToString("N2"),
                    running.ToString("N2"));
            }

            _balance.Text = $"الرصيد: {running:N2}";
            _inTotal.Text = $"إجمالي الوارد: {incoming:N2}";
            _outTotal.Text = $"إجمالي المنصرف: {outgoing:N2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل حركة الخزينة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool MatchesType(string value)
    {
        var selected = _type.SelectedIndex;
        if (selected <= 0) return true;
        return selected switch
        {
            1 => value == "مبيعات",
            2 => value == "مشتريات",
            3 => value == "صرف",
            4 => value == "توريد",
            _ => true
        };
    }

    private static string NormalizeType(string value)
    {
        if (value is "إيداع" or "قبض" or "توريد")
            return "توريد";
        if (value is "سحب" or "صرف")
            return "صرف";
        if (value == "بيع")
            return "مبيعات";
        if (value == "شراء")
            return "مشتريات";
        return value;
    }

    private void AddTransaction(bool isOut)
    {
        using var dialog = new CashTransactionDialog(isOut);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            using var connection = Database.OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO cash_transactions
                    (transaction_date, transaction_type, reference_type, reference_id, amount_in, amount_out, notes)
                VALUES
                    ($date, $type, NULL, NULL, $amountIn, $amountOut, $notes);
                """;
            command.Parameters.AddWithValue("$date", dialog.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$type", dialog.TransactionType);
            command.Parameters.AddWithValue("$amountIn", isOut ? 0m : dialog.Amount);
            command.Parameters.AddWithValue("$amountOut", isOut ? dialog.Amount : 0m);
            command.Parameters.AddWithValue("$notes", dialog.Notes);
            command.ExecuteNonQuery();
            transaction.Commit();
            LoadTransactions();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر حفظ حركة الخزينة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CashForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            LoadTransactions();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.SuppressKeyPress = true;
        }
    }

    private static string FormatReference(string type, long id) => string.IsNullOrWhiteSpace(type) ? "" : type switch
    {
        "order" => $"فاتورة بيع #{id}",
        "purchase" => $"فاتورة شراء #{id}",
        "payment" => $"سداد #{id}",
        _ => id > 0 ? $"{type} #{id}" : type
    };

    private static bool TryParseDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 80000)
        {
            try { date = DateTime.FromOADate(serial); return true; } catch { }
        }
        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy", "MM/dd/yyyy", "MM/dd/yyyy HH:mm:ss" };
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date);
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
}

internal sealed class CashTransactionDialog : Form
{
    private readonly DateTimePicker _date = new();
    private readonly ComboBox _type = new();
    private readonly NumericUpDown _amount = new();
    private readonly TextBox _notes = new();
    private readonly bool _isOut;

    public DateTime TransactionDate => _date.Value;
    public decimal Amount => _amount.Value;
    public string Notes => _notes.Text.Trim();
    public string TransactionType => _type.SelectedItem?.ToString() ?? (_isOut ? "صرف" : "توريد");

    public CashTransactionDialog(bool isOut)
    {
        _isOut = isOut;
        Text = isOut ? "حركة صرف" : "حركة توريد";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 340);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(18), RightToLeft = RightToLeft.Yes };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

        _date.Dock = DockStyle.Fill;
        _date.Format = DateTimePickerFormat.Custom;
        _date.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _date.Value = DateTime.Now;

        _type.Dock = DockStyle.Fill;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Items.Add(isOut ? "صرف" : "توريد");
        _type.SelectedIndex = 0;

        _amount.Dock = DockStyle.Fill;
        _amount.DecimalPlaces = 2;
        _amount.Maximum = 1000000000;
        _amount.Minimum = 0.01m;
        _amount.ThousandsSeparator = true;

        _notes.Dock = DockStyle.Fill;
        _notes.Multiline = true;
        _notes.TextAlign = HorizontalAlignment.Right;
        _notes.ScrollBars = ScrollBars.Vertical;

        table.Controls.Add(MakeLabel("التاريخ"), 0, 0); table.Controls.Add(_date, 1, 0);
        table.Controls.Add(MakeLabel("نوع الحركة"), 0, 1); table.Controls.Add(_type, 1, 1);
        table.Controls.Add(MakeLabel("المبلغ"), 0, 2); table.Controls.Add(_amount, 1, 2);
        table.Controls.Add(MakeLabel("البيان"), 0, 3); table.Controls.Add(_notes, 1, 3);

        var save = new Button { Text = "حفظ", Dock = DockStyle.Fill, BackColor = isOut ? Color.FromArgb(192, 57, 43) : Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        save.Click += (_, _) => Save();
        table.Controls.Add(save, 0, 4); table.SetColumnSpan(save, 2);
        AcceptButton = save;

        Controls.Add(table);
    }

    private void Save()
    {
        if (_amount.Value <= 0)
        {
            MessageBox.Show(this, "المبلغ يجب أن يكون أكبر من صفر.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_notes.Text))
        {
            MessageBox.Show(this, "اكتب بيان الحركة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Label MakeLabel(string text) => new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
}
