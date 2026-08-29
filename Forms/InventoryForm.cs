using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Forms;

public sealed class InventoryForm : Form
{
    private readonly InventoryRepository _repository = new();
    private readonly TextBox _searchBox = new();
    private readonly DataGridView _stockGrid = new();
    private readonly DataGridView _ledgerGrid = new();
    private readonly Label _summaryLabel = new();

    public InventoryForm()
    {
        Text = "المخزون";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1250, 720);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);

        BuildUi();
        LoadStock();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 72, Padding = new Padding(12) };

        var title = new Label
        {
            Text = "المخزون",
            Dock = DockStyle.Right,
            Width = 160,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };

        _searchBox.Dock = DockStyle.Right;
        _searchBox.Width = 420;
        _searchBox.Font = new Font("Segoe UI", 11);
        _searchBox.PlaceholderText = "ابحث باسم الصنف أو الكود أو الباركود...";
        _searchBox.TextAlign = HorizontalAlignment.Right;
        _searchBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadStock();
                e.SuppressKeyPress = true;
            }
        };

        var searchButton = CreateButton("بحث", Color.FromArgb(52, 152, 219));
        searchButton.Dock = DockStyle.Right;
        searchButton.Width = 90;
        searchButton.Click += (_, _) => LoadStock();

        var refreshButton = CreateButton("تحديث", Color.FromArgb(127, 140, 141));
        refreshButton.Dock = DockStyle.Left;
        refreshButton.Width = 90;
        refreshButton.Click += (_, _) => LoadStock();

        header.Controls.Add(refreshButton);
        header.Controls.Add(_searchBox);
        header.Controls.Add(searchButton);
        header.Controls.Add(title);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 390,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(12)
        };

        ConfigureStockGrid();
        ConfigureLedgerGrid();

        var stockPanel = new Panel { Dock = DockStyle.Fill };
        _summaryLabel.Dock = DockStyle.Top;
        _summaryLabel.Height = 38;
        _summaryLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _summaryLabel.TextAlign = ContentAlignment.MiddleRight;
        stockPanel.Controls.Add(_stockGrid);
        stockPanel.Controls.Add(_summaryLabel);

        split.Panel1.Controls.Add(stockPanel);

        var ledgerTitle = new Label
        {
            Text = "سجل حركة المخزون للصنف المحدد",
            Dock = DockStyle.Top,
            Height = 38,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        split.Panel2.Controls.Add(_ledgerGrid);
        split.Panel2.Controls.Add(ledgerTitle);

        Controls.Add(split);
        Controls.Add(header);
    }

    private void ConfigureStockGrid()
    {
        _stockGrid.Dock = DockStyle.Fill;
        _stockGrid.BackgroundColor = Color.White;
        _stockGrid.BorderStyle = BorderStyle.None;
        _stockGrid.ReadOnly = true;
        _stockGrid.AllowUserToAddRows = false;
        _stockGrid.AllowUserToDeleteRows = false;
        _stockGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _stockGrid.MultiSelect = false;
        _stockGrid.AutoGenerateColumns = false;
        _stockGrid.RightToLeft = RightToLeft.Yes;
        _stockGrid.RowHeadersVisible = false;
        _stockGrid.RowTemplate.Height = 42;
        _stockGrid.SelectionChanged += (_, _) => LoadSelectedLedger();

        // Keep the database ProductId hidden in the row. The visible first
        // column is the product code (for example P00009), which must never be
        // parsed as the numeric database id.
        var productIdColumn = new DataGridViewTextBoxColumn
        {
            Name = "ProductId",
            HeaderText = "",
            DataPropertyName = "ProductId",
            Width = 0,
            Visible = false,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        _stockGrid.Columns.Add(productIdColumn);

        AddStockColumn("الكود", "Code", 100);
        AddStockColumn("الصنف", "ProductName", 400, fill: true);
        AddStockColumn("التصنيف", "Category", 150);
        AddStockColumn("الرصيد الافتتاحي", "OpeningStock", 120, "N2");
        AddStockColumn("المشتريات", "Purchased", 110, "N2");
        AddStockColumn("المبيعات", "Sold", 110, "N2");
        AddStockColumn("مرتجع مشتريات", "PurchaseReturns", 120, "N2");
        AddStockColumn("مرتجع مبيعات", "SalesReturns", 120, "N2");
        AddStockColumn("الرصيد الحالي", "CurrentStock", 120, "N2");
        AddStockColumn("الحالة", "Status", 100);
    }

    private void AddStockColumn(string header, string property, int width, string? format = null, bool fill = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        column.DefaultCellStyle.Alignment = property == "ProductName"
            ? DataGridViewContentAlignment.MiddleRight
            : DataGridViewContentAlignment.MiddleCenter;
        if (format is not null)
            column.DefaultCellStyle.Format = format;
        _stockGrid.Columns.Add(column);
    }

    private void ConfigureLedgerGrid()
    {
        _ledgerGrid.Dock = DockStyle.Fill;
        _ledgerGrid.BackgroundColor = Color.White;
        _ledgerGrid.BorderStyle = BorderStyle.None;
        _ledgerGrid.ReadOnly = true;
        _ledgerGrid.AllowUserToAddRows = false;
        _ledgerGrid.AutoGenerateColumns = false;
        _ledgerGrid.RightToLeft = RightToLeft.Yes;
        _ledgerGrid.RowHeadersVisible = false;
        _ledgerGrid.RowTemplate.Height = 36;

        AddLedgerColumn("التاريخ", "MovementDate", 120);
        AddLedgerColumn("الحركة", "MovementType", 150);
        AddLedgerColumn("مرجع", "ReferenceType", 130);
        AddLedgerColumn("رقم المرجع", "ReferenceId", 110);
        AddLedgerColumn("وارد", "QuantityIn", 100, "N2");
        AddLedgerColumn("منصرف", "QuantityOut", 100, "N2");
        AddLedgerColumn("تكلفة الوحدة", "UnitCost", 120, "N2");
        AddLedgerColumn("ملاحظات", "Notes", 300, fill: true);
    }

    private void AddLedgerColumn(string header, string property, int width, string? format = null, bool fill = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        column.DefaultCellStyle.Alignment = ContentAlignmentToGrid(property);
        if (format is not null)
            column.DefaultCellStyle.Format = format;
        _ledgerGrid.Columns.Add(column);
    }

    private static DataGridViewContentAlignment ContentAlignmentToGrid(string property)
        => property == "Notes" ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleCenter;

    private void LoadStock()
    {
        try
        {
            var rows = _repository.GetStock(_searchBox.Text);
            _stockGrid.DataSource = rows.Select(x => new
            {
                x.ProductId,
                x.Code,
                x.ProductName,
                x.Category,
                x.OpeningStock,
                x.Purchased,
                x.Sold,
                x.PurchaseReturns,
                x.SalesReturns,
                x.CurrentStock,
                Status = x.IsLowStock ? "منخفض" : "جيد"
            }).ToList();

            var totalProducts = rows.Count;
            var lowCount = rows.Count(x => x.IsLowStock);
            var totalUnits = rows.Sum(x => x.CurrentStock);
            _summaryLabel.Text = $"عدد الأصناف: {totalProducts:N0}    |    وحدات المخزون: {totalUnits:N2}    |    أصناف منخفضة: {lowCount:N0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل المخزون:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSelectedLedger()
    {
        _ledgerGrid.DataSource = null;
        if (_stockGrid.CurrentRow is null || _stockGrid.CurrentRow.IsNewRow)
            return;

        try
        {
            var productIdValue = _stockGrid.CurrentRow.Cells["ProductId"].Value;
            if (productIdValue is null || productIdValue == DBNull.Value)
                return;

            if (!long.TryParse(Convert.ToString(productIdValue), out var productId))
                return;

            _ledgerGrid.DataSource = _repository.GetLedger(productId).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل سجل الحركة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Button CreateButton(string text, Color color) => new()
    {
        Text = text,
        BackColor = color,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter
    };
}
