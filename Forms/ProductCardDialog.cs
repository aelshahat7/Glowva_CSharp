using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Forms;

/// <summary>
/// F3 product-card window.
/// Tab 1: core product data.
/// Tab 2: warehouse balances.
/// </summary>
public sealed class ProductCardDialog : Form
{
    private readonly long _productId;
    private readonly TabControl _tabs = new();
    private readonly TableLayoutPanel _basicTable = new();
    private readonly DataGridView _warehouseGrid = new();

    public ProductCardDialog(long productId)
    {
        _productId = productId;
        Text = "بيانات الصنف";
        ClientSize = new Size(760, 500);
        MinimumSize = new Size(700, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        KeyPreview = true;

        BuildUi();
        LoadProduct();
        LoadWarehouseBalances();
    }

    private void BuildUi()
    {
        var header = new Label
        {
            Text = "بيانات الصنف",
            Dock = DockStyle.Top,
            Height = 52,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(39, 130, 72),
            BackColor = Color.FromArgb(248, 248, 248)
        };

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = new Font("Segoe UI", 10);
        _tabs.RightToLeft = RightToLeft.Yes;

        var basicTab = new TabPage("بيانات الصنف")
        {
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        BuildBasicTab(basicTab);

        var warehouseTab = new TabPage("أرصدة المخازن")
        {
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        BuildWarehouseTab(warehouseTab);

        _tabs.TabPages.Add(basicTab);
        _tabs.TabPages.Add(warehouseTab);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(10, 5, 10, 5),
            BackColor = Color.FromArgb(248, 248, 248)
        };

        var close = new Button
        {
            Text = "إغلاق",
            Dock = DockStyle.Left,
            Width = 110,
            BackColor = Color.FromArgb(120, 120, 120),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        close.Click += (_, _) => Close();

        var edit = new Button
        {
            Text = "تعديل البيانات",
            Dock = DockStyle.Right,
            Width = 150,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        edit.Click += (_, _) => EditProduct();

        footer.Controls.Add(close);
        footer.Controls.Add(edit);

        Controls.Add(_tabs);
        Controls.Add(footer);
        Controls.Add(header);
    }

    private void BuildBasicTab(TabPage tab)
    {
        _basicTable.Dock = DockStyle.Fill;
        _basicTable.ColumnCount = 4;
        _basicTable.RowCount = 5;
        _basicTable.Padding = new Padding(6);
        _basicTable.RightToLeft = RightToLeft.Yes;

        _basicTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        _basicTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        _basicTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        _basicTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

        AddField(0, 0, "كود الصنف", "value_code");
        AddField(0, 2, "الكود المختصر", "value_short");
        AddField(1, 0, "الاسم العربي", "value_name_ar");
        AddField(1, 2, "الاسم الإنجليزي", "value_name_en");
        AddField(2, 0, "الباركود", "value_barcode");
        AddField(2, 2, "التصنيف", "value_category");
        AddField(3, 0, "سعر البيع", "value_sell");
        AddField(3, 2, "سعر الشراء", "value_buy");
        AddField(4, 0, "الرصيد الحالي", "value_stock");
        AddField(4, 2, "حد التنبيه", "value_low");

        tab.Controls.Add(_basicTable);
    }

    private void BuildWarehouseTab(TabPage tab)
    {
        _warehouseGrid.Dock = DockStyle.Fill;
        _warehouseGrid.ReadOnly = true;
        _warehouseGrid.AllowUserToAddRows = false;
        _warehouseGrid.AllowUserToDeleteRows = false;
        _warehouseGrid.AllowUserToResizeRows = false;
        _warehouseGrid.RowHeadersVisible = false;
        _warehouseGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _warehouseGrid.MultiSelect = false;
        _warehouseGrid.AutoGenerateColumns = false;
        _warehouseGrid.RightToLeft = RightToLeft.Yes;
        _warehouseGrid.BackgroundColor = Color.White;
        _warehouseGrid.BorderStyle = BorderStyle.None;
        _warehouseGrid.ScrollBars = ScrollBars.Both;
        _warehouseGrid.RowTemplate.Height = 40;
        _warehouseGrid.ColumnHeadersHeight = 38;
        _warehouseGrid.EnableHeadersVisualStyles = false;
        _warehouseGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        AddWarehouseColumn("المخزن", "Warehouse", 260);
        AddWarehouseColumn("الرصيد الحالي", "Balance", 180, "N2");
        AddWarehouseColumn("حد التنبيه", "LowStock", 160, "N2");

        tab.Controls.Add(_warehouseGrid);
    }

    private void AddField(int row, int labelColumn, string label, string valueName)
    {
        var valueColumn = labelColumn + 1;
        var labelControl = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(4)
        };
        var valueControl = new Label
        {
            Name = valueName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(4),
            BackColor = Color.White
        };

        _basicTable.Controls.Add(labelControl, labelColumn, row);
        _basicTable.Controls.Add(valueControl, valueColumn, row);
    }

    private void AddWarehouseColumn(string header, string property, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = format ?? string.Empty
            }
        };
        _warehouseGrid.Columns.Add(column);
    }

    private void LoadProduct()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.name, p.code, p.barcode, p.category,
                   p.sell_price, p.buy_price, p.low_stock_threshold,
                   p.opening_stock + COALESCE(
                       (SELECT SUM(il.quantity_in - il.quantity_out)
                        FROM inventory_ledger il
                        WHERE il.product_id = p.id), 0
                   ) AS current_stock
            FROM products p
            WHERE p.id = $id;
            """;
        command.Parameters.AddWithValue("$id", _productId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            MessageBox.Show(this, "لم يتم العثور على الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
            return;
        }

        SetValue("value_code", reader.IsDBNull(1) ? "" : reader.GetString(1));
        SetValue("value_short", reader.IsDBNull(1) ? "" : reader.GetString(1));
        SetValue("value_name_ar", reader.IsDBNull(0) ? "" : reader.GetString(0));
        SetValue("value_name_en", "");
        SetValue("value_barcode", reader.IsDBNull(2) ? "" : reader.GetString(2));
        SetValue("value_category", reader.IsDBNull(3) ? "" : reader.GetString(3));
        SetValue("value_sell", reader.IsDBNull(4) ? "0.00" : Convert.ToDecimal(reader.GetValue(4)).ToString("N2"));
        SetValue("value_buy", reader.IsDBNull(5) ? "0.00" : Convert.ToDecimal(reader.GetValue(5)).ToString("N2"));
        SetValue("value_stock", reader.IsDBNull(7) ? "0.00" : Convert.ToDecimal(reader.GetValue(7)).ToString("N2"));
        SetValue("value_low", reader.IsDBNull(6) ? "0.00" : Convert.ToDecimal(reader.GetValue(6)).ToString("N2"));
    }

    private void LoadWarehouseBalances()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 'المخزن الرئيسي' AS Warehouse,
                   p.opening_stock + COALESCE(
                       (SELECT SUM(il.quantity_in - il.quantity_out)
                        FROM inventory_ledger il
                        WHERE il.product_id = p.id), 0
                   ) AS Balance,
                   p.low_stock_threshold AS LowStock
            FROM products p
            WHERE p.id = $id;
            """;
        command.Parameters.AddWithValue("$id", _productId);

        var rows = new List<object>();
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            rows.Add(new
            {
                Warehouse = reader.GetString(0),
                Balance = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                LowStock = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2))
            });
        }

        _warehouseGrid.DataSource = rows;
    }

    private void EditProduct()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, code, name, barcode, category, sell_price, buy_price, opening_stock, low_stock_threshold, is_active FROM products WHERE id = $id;";
            command.Parameters.AddWithValue("$id", _productId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                MessageBox.Show(this, "لم يتم العثور على الصنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var product = new Product
            {
                Id = reader.GetInt64(0),
                Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Barcode = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                SellPrice = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
                BuyPrice = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)),
                OpeningStock = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
                LowStockThreshold = reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8)),
                IsActive = !reader.IsDBNull(9) && Convert.ToInt32(reader.GetValue(9)) != 0
            };

            using var editor = new ProductEditorForm(product, isNew: false);
            if (editor.ShowDialog(this) != DialogResult.OK)
                return;

            var repository = new ProductRepository();
            repository.Update(editor.Product);
            LoadProduct();
            LoadWarehouseBalances();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تعديل الصنف:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetValue(string name, string value)
    {
        if (_basicTable.Controls.Find(name, true).FirstOrDefault() is Label label)
            label.Text = value;
    }
}
