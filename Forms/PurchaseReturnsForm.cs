using GlowvaERP.Data;
using GlowvaERP.Models;
using GlowvaERP.Services;
using System.Globalization;

namespace GlowvaERP.Forms;

public sealed class PurchaseReturnsForm : Form
{
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly CheckBox _allDates = new();
    private readonly ComboBox _supplier = new();
    private readonly ComboBox _payment = new();
    private readonly TextBox _invoice = new();
    private readonly TextBox _product = new();
    private readonly DataGridView _results = new();
    private readonly DataGridView _itemsGrid = new();
    private readonly TextBox _reason = new();
    private readonly Label _total = new();
    private readonly Label _searchStatus = new();
    private readonly PurchaseReturnService _service = new();
    private readonly List<PurchaseReturnItemDraft> _items = new();
    private long _selectedPurchaseId;
    private long? _selectedSupplierId;

    public PurchaseReturnsForm()
    {
        Text = "مرتجعات المشتريات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1250, 760);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        BackColor = Color.FromArgb(248, 248, 248);
        BuildUi();
        LoadSuppliers();
    }

    private void BuildUi()
    {
        var filters = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 155, ColumnCount = 10, RowCount = 2,
            Padding = new Padding(12), RightToLeft = RightToLeft.Yes
        };
        for (int i = 0; i < 10; i++) filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10f));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        filters.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        filters.Controls.Add(MakeLabel("من التاريخ"), 0, 0);
        filters.Controls.Add(_from, 1, 0);
        filters.Controls.Add(MakeLabel("إلى التاريخ"), 2, 0);
        filters.Controls.Add(_to, 3, 0);
        filters.Controls.Add(_allDates, 4, 0);
        filters.Controls.Add(MakeLabel("المورد"), 5, 0);
        filters.Controls.Add(_supplier, 6, 0);
        filters.Controls.Add(MakeLabel("طريقة الدفع"), 7, 0);
        filters.Controls.Add(_payment, 8, 0);

        _from.Format = _to.Format = DateTimePickerFormat.Custom;
        _from.CustomFormat = _to.CustomFormat = "yyyy-MM-dd";
        _from.Value = DateTime.Today.AddYears(-5);
        _to.Value = DateTime.Today;

        _allDates.Text = "كل التواريخ";
        _allDates.AutoSize = true;
        _allDates.Checked = true;
        _allDates.RightToLeft = RightToLeft.Yes;
        _allDates.CheckedChanged += (_, _) => UpdateDateFilterState();

        _supplier.DropDownStyle = ComboBoxStyle.DropDownList;
        _supplier.RightToLeft = RightToLeft.Yes;
        _payment.Items.AddRange(new object[] { "الكل", "مدفوع", "آجل" });
        _payment.SelectedIndex = 0;
        _payment.DropDownStyle = ComboBoxStyle.DropDownList;
        _payment.RightToLeft = RightToLeft.Yes;

        filters.Controls.Add(MakeLabel("رقم الفاتورة"), 0, 1);
        filters.Controls.Add(_invoice, 1, 1);
        filters.Controls.Add(MakeLabel("الصنف"), 2, 1);
        filters.Controls.Add(_product, 3, 1);

        var search = new Button { Text = "بحث", Dock = DockStyle.Fill, BackColor = Color.FromArgb(52,152,219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        search.Click += (_, _) => SearchPurchases();
        filters.Controls.Add(search, 5, 1);

        var clear = new Button { Text = "مسح الفلاتر", Dock = DockStyle.Fill, BackColor = Color.FromArgb(120,120,120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        clear.Click += (_, _) => ClearFilters();
        filters.Controls.Add(clear, 6, 1);

        var select = new Button { Text = "اختيار الفاتورة", Dock = DockStyle.Fill, BackColor = Color.FromArgb(39,174,96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        select.Click += (_, _) => LoadSelectedPurchase();
        filters.Controls.Add(select, 7, 1);

        _searchStatus.Text = "فعّل كل التواريخ للبحث في جميع الفواتير، أو ألغِها لاستخدام فترة محددة.";
        _searchStatus.Dock = DockStyle.Fill;
        _searchStatus.TextAlign = ContentAlignment.MiddleRight;
        _searchStatus.ForeColor = Color.DimGray;
        _searchStatus.RightToLeft = RightToLeft.Yes;
        filters.Controls.Add(_searchStatus, 8, 1);
        filters.SetColumnSpan(_searchStatus, 2);

        ConfigureResults();
        ConfigureItems();

        var topSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 6 };
        topSplit.Resize += (_, _) => SetSafeSplitterDistance(topSplit, 300);
        topSplit.Panel1.Controls.Add(_results);

        var bottom = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        var reasonLabel = MakeLabel("سبب المرتجع");
        reasonLabel.Dock = DockStyle.Top; reasonLabel.Height = 28;
        _reason.Dock = DockStyle.Top; _reason.Height = 36; _reason.TextAlign = HorizontalAlignment.Right; _reason.RightToLeft = RightToLeft.Yes;
        _total.Text = "الإجمالي: 0.00"; _total.Font = new Font("Segoe UI", 14, FontStyle.Bold); _total.AutoSize = true; _total.Dock = DockStyle.Left;
        var save = new Button { Text = "حفظ مرتجع المشتريات", Dock = DockStyle.Right, Width = 230, Height = 42, BackColor = Color.FromArgb(192,57,43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        save.Click += (_, _) => SaveReturn();
        bottom.Controls.Add(_itemsGrid); bottom.Controls.Add(save); bottom.Controls.Add(_total); bottom.Controls.Add(_reason); bottom.Controls.Add(reasonLabel);
        topSplit.Panel2.Controls.Add(bottom);

        Controls.Add(topSplit); Controls.Add(filters);
        Shown += (_, _) => SetSafeSplitterDistance(topSplit, 300);
        Shown += (_, _) => SearchPurchases();
    }

    private void UpdateDateFilterState()
    {
        _from.Enabled = !_allDates.Checked;
        _to.Enabled = !_allDates.Checked;
    }

    private void ClearFilters()
    {
        _invoice.Clear(); _product.Clear(); _supplier.SelectedIndex = 0; _payment.SelectedIndex = 0;
        _allDates.Checked = true; _from.Value = DateTime.Today.AddYears(-5); _to.Value = DateTime.Today;
        _results.Rows.Clear(); _searchStatus.Text = "تم مسح الفلاتر. اضغط بحث لإظهار الفواتير."; ClearSelectedPurchase();
    }

    private static void SetSafeSplitterDistance(SplitContainer split, int preferred)
    {
        const int panel1Min = 180, panel2Min = 220;
        int max = split.ClientSize.Height - split.SplitterWidth - panel2Min;
        if (max < panel1Min) return;
        split.Panel1MinSize = panel1Min; split.Panel2MinSize = panel2Min;
        split.SplitterDistance = Math.Clamp(preferred, panel1Min, max);
    }

    private void ConfigureResults()
    {
        _results.Dock = DockStyle.Fill; _results.ReadOnly = true; _results.AllowUserToAddRows = false; _results.RowHeadersVisible = false;
        _results.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _results.MultiSelect = false; _results.RightToLeft = RightToLeft.Yes;
        _results.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _results.BackgroundColor = Color.White;
        _results.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "رقم الفاتورة", Name = "invoice", FillWeight = 16 },
            new DataGridViewTextBoxColumn { HeaderText = "التاريخ", Name = "date", FillWeight = 18 },
            new DataGridViewTextBoxColumn { HeaderText = "المورد", Name = "supplier", FillWeight = 22 },
            new DataGridViewTextBoxColumn { HeaderText = "الدفع", Name = "payment", FillWeight = 14 },
            new DataGridViewTextBoxColumn { HeaderText = "الإجمالي", Name = "total", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "الأصناف", Name = "items", FillWeight = 15 });
        _results.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) LoadSelectedPurchase(); };
    }

    private void ConfigureItems()
    {
        _itemsGrid.Dock = DockStyle.Fill; _itemsGrid.AllowUserToAddRows = false; _itemsGrid.RowHeadersVisible = false; _itemsGrid.RightToLeft = RightToLeft.Yes;
        _itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _itemsGrid.BackgroundColor = Color.White; _itemsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _itemsGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "الصنف", Name = "name", FillWeight = 32, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "الكمية الأصلية", Name = "purchased", FillWeight = 16, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "مرتجع سابق", Name = "returned", FillWeight = 14, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "المتاح", Name = "available", FillWeight = 14, ReadOnly = true },
            new DataGridViewTextBoxColumn { HeaderText = "كمية المرتجع", Name = "qty", FillWeight = 12 },
            new DataGridViewTextBoxColumn { HeaderText = "سعر الشراء", Name = "price", FillWeight = 12, ReadOnly = true });
        _itemsGrid.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _items.Count || e.ColumnIndex != 4) return;
            if (!decimal.TryParse(_itemsGrid.Rows[e.RowIndex].Cells[4].Value?.ToString(), out var qty)) qty = 0;
            qty = Math.Max(0, Math.Min(qty, _items[e.RowIndex].AvailableToReturn));
            _items[e.RowIndex].ReturnQuantity = qty;
            _itemsGrid.Rows[e.RowIndex].Cells[4].Value = qty.ToString("N2"); UpdateTotal();
        };
    }

    private void LoadSuppliers()
    {
        try
        {
            using var c = Database.OpenConnection(); using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM suppliers WHERE is_active=1 ORDER BY name;";
            var list = new List<SupplierChoice> { new(0, "الكل") };
            using var r = cmd.ExecuteReader(); while (r.Read()) list.Add(new SupplierChoice(r.GetInt64(0), r.IsDBNull(1) ? "" : r.GetString(1)));
            _supplier.DataSource = list; _supplier.DisplayMember = nameof(SupplierChoice.Name); _supplier.ValueMember = nameof(SupplierChoice.Id); _supplier.SelectedIndex = 0;
            UpdateDateFilterState();
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر تحميل الموردين:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SearchPurchases()
    {
        try
        {
            using var c = Database.OpenConnection(); using var cmd = c.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrWhiteSpace(_invoice.Text)) { where.Add("CAST(p.invoice_number AS TEXT) LIKE $invoice"); cmd.Parameters.AddWithValue("$invoice", $"%{_invoice.Text.Trim()}%"); }
            var supplierId = Convert.ToInt64(_supplier.SelectedValue ?? 0);
            if (supplierId > 0) { where.Add("p.supplier_id = $supplierId"); cmd.Parameters.AddWithValue("$supplierId", supplierId); }
            if (!string.IsNullOrWhiteSpace(_payment.Text) && _payment.Text != "الكل") { where.Add("p.payment_status = $payment"); cmd.Parameters.AddWithValue("$payment", _payment.Text); }
            if (!string.IsNullOrWhiteSpace(_product.Text)) { where.Add("EXISTS (SELECT 1 FROM purchase_items pi2 JOIN products p2 ON p2.id=pi2.product_id WHERE pi2.purchase_id=p.id AND (p2.name LIKE $product OR p2.code LIKE $product OR p2.barcode LIKE $product))"); cmd.Parameters.AddWithValue("$product", $"%{_product.Text.Trim()}%"); }

            cmd.CommandText = $"""
                SELECT p.id, p.invoice_number, p.purchase_date, COALESCE(s.name,'مورد نقدي'), COALESCE(p.payment_status,'مدفوع'),
                       ROUND(COALESCE(SUM(pi.quantity*pi.unit_price),0) - COALESCE(p.discount,0),2), COUNT(pi.id)
                FROM purchases p LEFT JOIN suppliers s ON s.id=p.supplier_id LEFT JOIN purchase_items pi ON pi.purchase_id=p.id
                {(where.Count == 0 ? "" : "WHERE " + string.Join(" AND ", where))}
                GROUP BY p.id ORDER BY p.id DESC;
                """;

            var rows = new List<(long Id, long Invoice, DateTime Date, string Supplier, string Payment, decimal Total, int Items)>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var rawDate = r.IsDBNull(2) ? null : r.GetValue(2)?.ToString();
                if (!TryParseLegacyDate(rawDate, out var purchaseDate)) purchaseDate = DateTime.MinValue;
                rows.Add((r.GetInt64(0), Convert.ToInt64(r.GetValue(1)), purchaseDate,
                    r.IsDBNull(3) ? "مورد نقدي" : r.GetString(3), r.IsDBNull(4) ? "مدفوع" : r.GetString(4),
                    Convert.ToDecimal(r.GetValue(5)), Convert.ToInt32(r.GetValue(6))));
            }

            var filtered = _allDates.Checked ? rows : rows.Where(x => x.Date != DateTime.MinValue && x.Date.Date >= _from.Value.Date && x.Date.Date <= _to.Value.Date).ToList();
            _results.Rows.Clear();
            foreach (var row in filtered)
                _results.Rows.Add(row.Invoice, row.Date == DateTime.MinValue ? "غير معروف" : row.Date.ToString("yyyy-MM-dd"), row.Supplier, row.Payment, row.Total.ToString("N2"), row.Items);
            _searchStatus.Text = filtered.Count == 0 ? "لا توجد فواتير مطابقة للفلاتر الحالية. جرّب كل التواريخ بدون مورد أو صنف." : $"تم العثور على {filtered.Count} فاتورة.";
            ClearSelectedPurchase();
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر البحث:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static bool TryParseLegacyDate(string? value, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 20000 && serial < 80000)
        {
            try { date = DateTime.FromOADate(serial); return true; } catch { }
        }
        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy" };
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out date)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out date);
    }

    private void LoadSelectedPurchase()
    {
        if (_results.CurrentRow == null) return;
        var invoice = _results.CurrentRow.Cells["invoice"].Value?.ToString(); if (string.IsNullOrWhiteSpace(invoice)) return;
        try
        {
            using var c = Database.OpenConnection(); using var cmd = c.CreateCommand();
            cmd.CommandText = """
                SELECT pi.id, pi.product_id, p.name, pi.quantity, pi.unit_price,
                       COALESCE((SELECT SUM(pri.quantity) FROM purchase_return_items pri JOIN purchase_returns pr ON pr.id=pri.purchase_return_id WHERE pri.purchase_item_id=pi.id),0), pu.supplier_id
                FROM purchase_items pi JOIN products p ON p.id=pi.product_id JOIN purchases pu ON pu.id=pi.purchase_id
                WHERE pu.invoice_number=$invoice ORDER BY pi.id;
                """;
            cmd.Parameters.AddWithValue("$invoice", invoice);
            _items.Clear();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                _selectedPurchaseId = GetPurchaseIdByInvoice(invoice); _selectedSupplierId = r.IsDBNull(6) ? null : r.GetInt64(6);
                _items.Add(new PurchaseReturnItemDraft { PurchaseItemId=r.GetInt64(0), ProductId=r.GetInt64(1), ProductName=r.IsDBNull(2)?"":r.GetString(2), PurchasedQuantity=Convert.ToDecimal(r.GetValue(3)), UnitPrice=Convert.ToDecimal(r.GetValue(4)), AlreadyReturned=Convert.ToDecimal(r.GetValue(5)) });
            }
            RefreshItems();
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر تحميل الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private long GetPurchaseIdByInvoice(string invoice)
    {
        using var c = Database.OpenConnection(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id FROM purchases WHERE invoice_number=$invoice LIMIT 1;"; cmd.Parameters.AddWithValue("$invoice", invoice);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private void RefreshItems()
    {
        _itemsGrid.Rows.Clear(); foreach (var x in _items) _itemsGrid.Rows.Add(x.ProductName, x.PurchasedQuantity, x.AlreadyReturned, x.AvailableToReturn, x.ReturnQuantity, x.UnitPrice); UpdateTotal();
    }

    private void UpdateTotal() => _total.Text = $"الإجمالي: {_items.Sum(x => x.Total):N2}";

    private void ClearSelectedPurchase()
    {
        _selectedPurchaseId = 0; _selectedSupplierId = null; _items.Clear(); _itemsGrid.Rows.Clear(); _reason.Clear(); UpdateTotal();
    }

    private void SaveReturn()
    {
        var selected = _items.Where(x => x.ReturnQuantity > 0).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, "حدد كمية مرتجع واحدة على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try
        {
            var id = _service.SaveReturn(_selectedPurchaseId, _selectedSupplierId, _reason.Text, selected);
            MessageBox.Show(this, $"تم حفظ المرتجع بنجاح.\nرقم العملية: {id}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _reason.Clear(); SearchPurchases();
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر حفظ المرتجع:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static Label MakeLabel(string text) => new() { Text=text, Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleRight, RightToLeft=RightToLeft.Yes };
    private sealed record SupplierChoice(long Id, string Name);
}
