using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Helpers;

namespace GlowvaERP.Forms;

public sealed class ReportViewerForm : Form
{
    private readonly string _title;
    private readonly string _sql;
    private readonly DataGridView _grid = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly Label _summary = new();

    public ReportViewerForm(string title, string sql)
    {
        _title = title;
        _sql = sql;
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1150, 700);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        KeyPreview = true;
        BuildUi();
        LoadReport();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5) { LoadReport(); return true; }
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(4),
            BackColor = Color.FromArgb(248, 248, 248)
        };
        _from.Format = DateTimePickerFormat.Short;
        _from.Value = DateTime.Today.AddMonths(-1);
        _to.Format = DateTimePickerFormat.Short;
        _to.Value = DateTime.Today;
        var search = new Button { Text = "بحث", Width = 100, Height = 32, BackColor = Color.FromArgb(33, 150, 243), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        search.Click += (_, _) => LoadReport();
        var close = new Button { Text = "إغلاق", Width = 100, Height = 32, BackColor = Color.FromArgb(90, 90, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        close.Click += (_, _) => Close();
        top.Controls.Add(close);
        top.Controls.Add(search);
        top.Controls.Add(_to);
        top.Controls.Add(new Label { Text = "إلى", AutoSize = true, Padding = new Padding(6, 8, 6, 0) });
        top.Controls.Add(_from);
        top.Controls.Add(new Label { Text = "من", AutoSize = true, Padding = new Padding(6, 8, 6, 0) });
        top.Controls.Add(new Label { Text = _title, AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), Padding = new Padding(16, 4, 16, 0) });

        ScrollableLayout.ConfigureGrid(_grid, 36);
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = true;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;

        _summary.Dock = DockStyle.Fill;
        _summary.TextAlign = ContentAlignment.MiddleRight;
        _summary.Font = new Font("Segoe UI", 10, FontStyle.Bold);

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(_summary, 0, 2);
        Controls.Add(root);
    }

    private void LoadReport()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = _sql;
            command.Parameters.AddWithValue("$from", _from.Value.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("$to", _to.Value.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            var table = new System.Data.DataTable();
            for (var i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            var count = 0;
            while (reader.Read())
            {
                var row = table.NewRow();
                for (var i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                table.Rows.Add(row);
                count++;
            }
            _grid.DataSource = table;
            _summary.Text = $"عدد النتائج: {count:N0}    |    الفترة: {_from.Value:yyyy/MM/dd} إلى {_to.Value:yyyy/MM/dd}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل التقرير:\n{ex.Message}", _title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
