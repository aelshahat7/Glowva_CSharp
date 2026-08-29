using GlowvaERP.Data;

namespace GlowvaERP.Forms;

public sealed class ProductSearchDialog : Form
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _search = new();
    public long? SelectedProductId { get; private set; }
    public string? SelectedProductName { get; private set; }

    public ProductSearchDialog() : this(null)
    {
    }

    public ProductSearchDialog(string? initialSearch)
    {
        Text = "بحث الأصناف";
        Size = new Size(900, 600);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterParent;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        KeyPreview = true;
        BuildUi(initialSearch ?? "");
        LoadProducts(initialSearch ?? "");
    }

    private void BuildUi(string initialSearch)
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(10) };
        var label = new Label { Text = "بحث الصنف / الكود / الباركود", Dock = DockStyle.Right, Width = 210, TextAlign = ContentAlignment.MiddleRight };
        _search.Dock = DockStyle.Fill;
        _search.Text = initialSearch;
        _search.TextAlign = HorizontalAlignment.Right;
        _search.RightToLeft = RightToLeft.Yes;
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadProducts(_search.Text.Trim());
                e.SuppressKeyPress = true;
            }
        };
        top.Controls.Add(_search);
        top.Controls.Add(label);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الصنف", Name = "name", FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكود", Name = "code", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الباركود", Name = "barcode", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "السعر", Name = "price", FillWeight = 20 });
        _grid.DoubleClick += (_, _) => AcceptSelection();
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

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };
        var accept = new Button { Text = "اختيار", Dock = DockStyle.Right, Width = 110 };
        var cancel = new Button { Text = "إلغاء", Dock = DockStyle.Right, Width = 110 };
        accept.Click += (_, _) => AcceptSelection();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(accept);

        Controls.Add(_grid);
        Controls.Add(bottom);
        Controls.Add(top);
    }

    private void LoadProducts(string query)
    {
        _grid.Rows.Clear();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, code, name, barcode, sell_price
            FROM products
            WHERE is_active = 1
              AND ($q = '' OR name LIKE $qLike OR code LIKE $qLike OR barcode LIKE $qLike)
            ORDER BY name
            LIMIT 300;
            """;
        command.Parameters.AddWithValue("$q", query);
        command.Parameters.AddWithValue("$qLike", $"%{query}%");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            _grid.Rows.Add(
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4)));
            _grid.Rows[^1].Tag = reader.GetInt64(0);
        }
    }

    private void AcceptSelection()
    {
        if (_grid.CurrentRow == null || _grid.CurrentRow.Tag is not long id)
            return;

        SelectedProductId = id;
        SelectedProductName = _grid.CurrentRow.Cells["name"].Value?.ToString();
        DialogResult = DialogResult.OK;
        Close();
    }
}
