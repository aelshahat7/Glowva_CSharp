using System.Drawing;
using System.Windows.Forms;
using GlowvaERP.Data;

namespace GlowvaERP.Forms;

/// <summary>
/// Compact selector used by F1 when the focused field is a customer or supplier.
/// </summary>
public sealed class PartySearchDialog : Form
{
    private readonly bool _supplierMode;
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();

    public long? SelectedId { get; private set; }
    public string? SelectedName { get; private set; }

    public PartySearchDialog(bool supplierMode, string? initialSearch = null)
    {
        _supplierMode = supplierMode;
        Text = supplierMode ? "بحث الموردين" : "بحث العملاء";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 480);
        Size = new Size(900, 560);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        KeyPreview = true;
        BuildUi(initialSearch ?? string.Empty);
        LoadRows(initialSearch ?? string.Empty);
    }

    private void BuildUi(string initialSearch)
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        var searchRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            RightToLeft = RightToLeft.Yes
        };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        searchRow.Controls.Add(new Label
        {
            Text = _supplierMode ? "المورد" : "العميل",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        }, 0, 0);

        _search.Dock = DockStyle.Fill;
        _search.Margin = new Padding(3);
        _search.Text = initialSearch;
        _search.TextAlign = HorizontalAlignment.Right;
        _search.RightToLeft = RightToLeft.Yes;
        _search.PlaceholderText = "ابحث بالاسم أو الكود أو الهاتف";
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadRows(_search.Text.Trim());
                e.SuppressKeyPress = true;
            }
        };
        searchRow.Controls.Add(_search, 1, 0);

        var searchButton = CreateButton("بحث", Color.FromArgb(52, 152, 219));
        searchButton.Dock = DockStyle.Fill;
        searchButton.Click += (_, _) => LoadRows(_search.Text.Trim());
        searchRow.Controls.Add(searchButton, 2, 0);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.RowTemplate.Height = 34;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "الكود", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "الاسم", FillWeight = 42 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "الهاتف", FillWeight = 22 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "الرصيد", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _grid.CellDoubleClick += (_, _) => AcceptSelection();
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AcceptSelection();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            RightToLeft = RightToLeft.Yes
        };
        var accept = CreateButton("اختيار", Color.FromArgb(39, 174, 96));
        accept.Width = 110;
        accept.Click += (_, _) => AcceptSelection();
        var cancel = CreateButton("إلغاء", Color.FromArgb(100, 100, 100));
        cancel.Width = 110;
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(accept);
        buttons.Controls.Add(cancel);

        root.Controls.Add(searchRow, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        Controls.Add(root);
    }

    private void LoadRows(string query)
    {
        _grid.Rows.Clear();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var table = _supplierMode ? "suppliers" : "customers";
        command.CommandText = $"""
            SELECT id, code, name, phone, opening_balance
            FROM {table}
            WHERE is_active = 1
              AND ($q = '' OR name LIKE $like OR code LIKE $like OR phone LIKE $like)
            ORDER BY name
            LIMIT 300;
            """;
        command.Parameters.AddWithValue("$q", query);
        command.Parameters.AddWithValue("$like", $"%{query}%");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _grid.Rows.Add(
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4)));
            _grid.Rows[^1].Tag = reader.GetInt64(0);
        }
    }

    private void AcceptSelection()
    {
        if (_grid.CurrentRow?.Tag is not long id)
            return;

        SelectedId = id;
        SelectedName = Convert.ToString(_grid.CurrentRow.Cells["Name"].Value);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Button CreateButton(string text, Color color) => new()
    {
        Text = text,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
    };
}
