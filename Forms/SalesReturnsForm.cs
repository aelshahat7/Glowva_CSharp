using GlowvaERP.Data;
using GlowvaERP.Models;
using GlowvaERP.Services;

namespace GlowvaERP.Forms;

public sealed class SalesReturnsForm : Form
{
    private readonly ComboBox _customer = new();
    private readonly ComboBox _paymentStatus = new();
    private readonly DateTimePicker _dateFrom = new();
    private readonly DateTimePicker _dateTo = new();
    private readonly CheckBox _allDates = new();
    private readonly TextBox _invoiceNumber = new();
    private readonly TextBox _productSearch = new();
    private readonly DataGridView _invoiceGrid = new();
    private readonly Label _searchSummary = new();

    private readonly DataGridView _itemsGrid = new();
    private readonly TextBox _reason = new();
    private readonly Label _total = new();
    private readonly SalesReturnService _service = new();
    private readonly List<SalesReturnItemDraft> _items = new();

    private long _selectedOrderId;
    private long? _selectedCustomerId;

    private sealed record OrderSearchRow(long Id, long InvoiceNumber, DateTime OrderDate, string CustomerName, string PaymentStatus, decimal Total, string ItemsSummary);

    public SalesReturnsForm()
    {
        Text = "مرتجعات المبيعات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1250, 820);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        BuildUi();
        LoadCustomers();
        SetDefaultDates();
    }

    private void BuildUi()
    {
        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(16), BackColor = Color.FromArgb(232, 232, 232), RightToLeft = RightToLeft.Yes };
        var title = new Label { Text = "البحث عن فاتورة مبيعات للمرتجع", Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight, RightToLeft = RightToLeft.Yes };
        var customerLabel = new Label { Text = "العميل", AutoSize = true, Location = new Point(1080, 52) };
        _customer.Location = new Point(820, 47); _customer.Width = 240; _customer.DropDownStyle = ComboBoxStyle.DropDownList; _customer.RightToLeft = RightToLeft.Yes;
        var paymentLabel = new Label { Text = "طريقة الدفع", AutoSize = true, Location = new Point(760, 52) };
        _paymentStatus.Location = new Point(620, 47); _paymentStatus.Width = 120; _paymentStatus.DropDownStyle = ComboBoxStyle.DropDownList; _paymentStatus.Items.AddRange(new object[] { "الكل", "مدفوع", "آجل" }); _paymentStatus.SelectedIndex = 0; _paymentStatus.RightToLeft = RightToLeft.Yes;
        var fromLabel = new Label { Text = "من", AutoSize = true, Location = new Point(560, 52) }; ConfigureDatePicker(_dateFrom, new Point(450, 47));
        var toLabel = new Label { Text = "إلى", AutoSize = true, Location = new Point(390, 52) }; ConfigureDatePicker(_dateTo, new Point(280, 47));
        _allDates.Text = "كل التواريخ"; _allDates.AutoSize = true; _allDates.Location = new Point(155, 51); _allDates.Checked = true; _allDates.RightToLeft = RightToLeft.Yes; _allDates.CheckedChanged += (_, _) => UpdateDateFilterState();
        var invoiceLabel = new Label { Text = "رقم الفاتورة", AutoSize = true, Location = new Point(205, 95) };
        _invoiceNumber.Location = new Point(105, 90); _invoiceNumber.Width = 90; _invoiceNumber.TextAlign = HorizontalAlignment.Right;
        var productLabel = new Label { Text = "الصنف داخل الفاتورة", AutoSize = true, Location = new Point(1080, 95) };
        _productSearch.Location = new Point(760, 90); _productSearch.Width = 300; _productSearch.PlaceholderText = "اسم الصنف / الكود / الباركود"; _productSearch.TextAlign = HorizontalAlignment.Right; _productSearch.RightToLeft = RightToLeft.Yes;
        var searchButton = CreateButton("بحث", Color.FromArgb(52, 152, 219), 125, 36); searchButton.Location = new Point(610, 130); searchButton.Click += (_, _) => SearchOrders();
        var clearButton = CreateButton("مسح الفلاتر", Color.FromArgb(120, 120, 120), 125, 36); clearButton.Location = new Point(470, 130); clearButton.Click += (_, _) => ClearFilters();
        _searchSummary.AutoSize = true; _searchSummary.Location = new Point(105, 145); _searchSummary.ForeColor = Color.DimGray; _searchSummary.Text = "اختر العميل أو أي فلتر ثم اضغط بحث. عند تفعيل كل التواريخ لن يتم تقييد البحث بالتاريخ.";
        searchPanel.Controls.AddRange(new Control[] { title, customerLabel, _customer, paymentLabel, _paymentStatus, fromLabel, _dateFrom, toLabel, _dateTo, _allDates, invoiceLabel, _invoiceNumber, productLabel, _productSearch, searchButton, clearButton, _searchSummary });

        ConfigureInvoiceGrid();
        var invoiceSelectPanel = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(12, 6, 12, 6), RightToLeft = RightToLeft.Yes };
        var chooseButton = CreateButton("اختيار الفاتورة المحددة", Color.FromArgb(52, 152, 219), 210, 34); chooseButton.Dock = DockStyle.Right; chooseButton.Click += (_, _) => LoadSelectedInvoiceFromGrid(); invoiceSelectPanel.Controls.Add(chooseButton);
        ConfigureItemsGrid();
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 120, Padding = new Padding(18), RightToLeft = RightToLeft.Yes };
        var reasonLabel = new Label { Text = "سبب المرتجع", AutoSize = true, Location = new Point(1110, 18) };
        _reason.Location = new Point(500, 10); _reason.Width = 590; _reason.Height = 38; _reason.TextAlign = HorizontalAlignment.Right; _reason.RightToLeft = RightToLeft.Yes;
        _total.AutoSize = true; _total.Font = new Font("Segoe UI", 15, FontStyle.Bold); _total.Location = new Point(85, 16); _total.Text = "الإجمالي: 0.00";
        var save = CreateButton("حفظ مرتجع المبيعات", Color.FromArgb(192, 57, 43), 250, 42); save.Location = new Point(55, 58); save.Click += (_, _) => SaveReturn();
        bottom.Controls.AddRange(new Control[] { reasonLabel, _reason, _total, save });

        Controls.Add(_itemsGrid); Controls.Add(bottom); Controls.Add(invoiceSelectPanel); Controls.Add(_invoiceGrid); Controls.Add(searchPanel);
    }

    private static void ConfigureDatePicker(DateTimePicker picker, Point location)
    {
        picker.Location = location; picker.Width = 105; picker.Format = DateTimePickerFormat.Custom; picker.CustomFormat = "dd/MM/yyyy"; picker.RightToLeft = RightToLeft.Yes; picker.RightToLeftLayout = false;
    }

    private void SetDefaultDates()
    {
        _dateTo.Value = DateTime.Today; _dateFrom.Value = DateTime.Today.AddYears(-1); UpdateDateFilterState();
    }

    private void UpdateDateFilterState() { _dateFrom.Enabled = !_allDates.Checked; _dateTo.Enabled = !_allDates.Checked; }

    private void ConfigureInvoiceGrid()
    {
        _invoiceGrid.Dock = DockStyle.Top; _invoiceGrid.Height = 225; _invoiceGrid.BackgroundColor = Color.White; _invoiceGrid.BorderStyle = BorderStyle.FixedSingle; _invoiceGrid.AllowUserToAddRows = false; _invoiceGrid.AllowUserToDeleteRows = false; _invoiceGrid.ReadOnly = true; _invoiceGrid.AutoGenerateColumns = false; _invoiceGrid.RightToLeft = RightToLeft.Yes; _invoiceGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _invoiceGrid.MultiSelect = false; _invoiceGrid.RowHeadersVisible = false; _invoiceGrid.RowTemplate.Height = 34;
        _invoiceGrid.CellDoubleClick += (_, _) => LoadSelectedInvoiceFromGrid();
        AddInvoiceColumn("رقم الفاتورة", "InvoiceNumber", 120); AddInvoiceColumn("التاريخ", "OrderDate", 120, "dd/MM/yyyy HH:mm"); AddInvoiceColumn("العميل", "CustomerName", 230); AddInvoiceColumn("طريقة الدفع", "PaymentStatus", 120); AddInvoiceColumn("الإجمالي", "Total", 130, "N2"); AddInvoiceColumn("الأصناف", "ItemsSummary", 520);
    }

    private void AddInvoiceColumn(string header, string property, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }; if (format != null) column.DefaultCellStyle.Format = format; _invoiceGrid.Columns.Add(column);
    }

    private void ConfigureItemsGrid()
    {
        _itemsGrid.Dock = DockStyle.Fill; _itemsGrid.BackgroundColor = Color.White; _itemsGrid.BorderStyle = BorderStyle.None; _itemsGrid.AllowUserToAddRows = false; _itemsGrid.AllowUserToDeleteRows = false; _itemsGrid.AutoGenerateColumns = false; _itemsGrid.RightToLeft = RightToLeft.Yes; _itemsGrid.RowHeadersVisible = false; _itemsGrid.RowTemplate.Height = 40; _itemsGrid.EditMode = DataGridViewEditMode.EditOnEnter; _itemsGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        AddItemColumn("الصنف", "ProductName", 500, true); AddItemColumn("السعر", "UnitPrice", 120, false, "N2"); AddItemColumn("الكمية الأصلية", "SoldQuantity", 130, false, "N2"); AddItemColumn("مرتجع سابق", "AlreadyReturned", 120, false, "N2"); AddItemColumn("المتاح للمرتجع", "AvailableToReturn", 130, false, "N2");
        var returnColumn = new DataGridViewTextBoxColumn { HeaderText = "كمية المرتجع", Name = "ReturnQuantity", Width = 140, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } }; _itemsGrid.Columns.Add(returnColumn);
        _itemsGrid.CellEndEdit += (_, e) => { if (e.RowIndex < 0 || e.RowIndex >= _items.Count || e.ColumnIndex != 5) return; var value = _itemsGrid.Rows[e.RowIndex].Cells[5].Value?.ToString(); if (!decimal.TryParse(value, out var qty)) qty = 0; qty = Math.Max(0m, Math.Min(qty, _items[e.RowIndex].AvailableToReturn)); _items[e.RowIndex].ReturnQuantity = qty; _itemsGrid.Rows[e.RowIndex].Cells[5].Value = qty.ToString("N2"); UpdateTotal(); };
    }

    private void AddItemColumn(string header, string property, int width, bool fill, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, Width = width, AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None, ReadOnly = true, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } }; if (format != null) column.DefaultCellStyle.Format = format; _itemsGrid.Columns.Add(column);
    }

    private void LoadCustomers()
    {
        try { using var connection = Database.OpenConnection(); using var command = connection.CreateCommand(); command.CommandText = "SELECT id, name FROM customers WHERE is_active = 1 ORDER BY name;"; var list = new List<PartyChoice> { new(0, "الكل") }; using var reader = command.ExecuteReader(); while (reader.Read()) list.Add(new PartyChoice(reader.GetInt64(0), reader.IsDBNull(1) ? "" : reader.GetString(1))); _customer.DataSource = list; _customer.DisplayMember = nameof(PartyChoice.Name); _customer.ValueMember = nameof(PartyChoice.Id); }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر تحميل العملاء:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SearchOrders()
    {
        try
        {
            using var connection = Database.OpenConnection(); using var command = connection.CreateCommand(); var conditions = new List<string>();
            if (!_allDates.Checked) { if (_dateFrom.Value.Date > _dateTo.Value.Date) { MessageBox.Show(this, "تاريخ البداية يجب أن يكون قبل تاريخ النهاية.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } conditions.Add("o.order_date >= $from"); conditions.Add("o.order_date < $to"); command.Parameters.AddWithValue("$from", _dateFrom.Value.Date.ToString("yyyy-MM-dd 00:00:00")); command.Parameters.AddWithValue("$to", _dateTo.Value.Date.AddDays(1).ToString("yyyy-MM-dd 00:00:00")); }
            var customerId = Convert.ToInt64(_customer.SelectedValue ?? 0); if (customerId > 0) { conditions.Add("o.customer_id = $customerId"); command.Parameters.AddWithValue("$customerId", customerId); }
            var payment = _paymentStatus.Text?.Trim(); if (!string.IsNullOrWhiteSpace(payment) && payment != "الكل") { conditions.Add("o.payment_status = $paymentStatus"); command.Parameters.AddWithValue("$paymentStatus", payment); }
            var invoiceText = _invoiceNumber.Text.Trim(); if (long.TryParse(invoiceText, out var invoiceNumber)) { conditions.Add("o.invoice_number = $invoiceNumber"); command.Parameters.AddWithValue("$invoiceNumber", invoiceNumber); } else if (!string.IsNullOrWhiteSpace(invoiceText)) { MessageBox.Show(this, "رقم الفاتورة يجب أن يكون رقمًا صحيحًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            var productQuery = _productSearch.Text.Trim(); if (!string.IsNullOrWhiteSpace(productQuery)) { conditions.Add("EXISTS (SELECT 1 FROM order_items oi2 JOIN products p2 ON p2.id = oi2.product_id WHERE oi2.order_id = o.id AND (p2.name LIKE $productQuery OR p2.code LIKE $productQuery OR p2.barcode LIKE $productQuery))"); command.Parameters.AddWithValue("$productQuery", $"%{productQuery}%"); }
            command.CommandText = $"""
                SELECT o.id, o.invoice_number, o.order_date, COALESCE(c.name, 'عميل نقدي') AS customer_name, o.payment_status,
                       ROUND(MAX(COALESCE((SELECT SUM(oi.quantity * oi.unit_price) FROM order_items oi WHERE oi.order_id = o.id), 0) - o.discount), 2) AS total,
                       COALESCE((SELECT GROUP_CONCAT(p.name, '، ') FROM order_items oi3 JOIN products p ON p.id = oi3.product_id WHERE oi3.order_id = o.id), '') AS items_summary
                FROM orders o LEFT JOIN customers c ON c.id = o.customer_id
                WHERE {(conditions.Count == 0 ? "1=1" : string.Join(" AND ", conditions))}
                GROUP BY o.id ORDER BY o.order_date DESC, o.id DESC LIMIT 500;
                """;
            var results = new List<OrderSearchRow>(); using var reader = command.ExecuteReader();
            while (reader.Read()) { var dateText = reader.GetString(2); var date = DateTime.TryParse(dateText, out var parsed) ? parsed : DateTime.MinValue; results.Add(new OrderSearchRow(reader.GetInt64(0), reader.GetInt64(1), date, reader.IsDBNull(3) ? "" : reader.GetString(3), reader.IsDBNull(4) ? "" : reader.GetString(4), Convert.ToDecimal(reader.GetValue(5)), reader.IsDBNull(6) ? "" : reader.GetString(6))); }
            _invoiceGrid.DataSource = results; _searchSummary.Text = results.Count == 0 ? "لا توجد فواتير مطابقة للفلاتر المحددة. إذا كانت القاعدة ما زالت بدون بيانات مبيعات، استورد البيانات القديمة أولًا." : $"تم العثور على {results.Count} فاتورة. اختر فاتورة ثم اضغط اختيار الفاتورة المحددة أو اضغط مرتين على الصف."; ClearSelectedInvoiceOnly();
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر البحث عن الفواتير:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ClearFilters()
    {
        _customer.SelectedIndex = 0; _paymentStatus.SelectedIndex = 0; _invoiceNumber.Clear(); _productSearch.Clear(); _allDates.Checked = true; SetDefaultDates(); _invoiceGrid.DataSource = null; _searchSummary.Text = "اختر العميل أو أي فلتر ثم اضغط بحث."; ClearSelectedInvoiceOnly();
    }

    private void LoadSelectedInvoiceFromGrid()
    {
        if (_invoiceGrid.CurrentRow?.DataBoundItem is not OrderSearchRow selected) { MessageBox.Show(this, "اختر فاتورة من قائمة نتائج البحث أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        LoadSelectedOrder(selected.Id, selected.InvoiceNumber);
    }

    private void LoadSelectedOrder(long orderId, long invoiceNumber)
    {
        try
        {
            _selectedOrderId = orderId; _items.Clear(); _reason.Clear(); using var connection = Database.OpenConnection(); using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT oi.id, oi.product_id, p.name, oi.quantity, oi.unit_price, oi.cost_price,
                       COALESCE((SELECT SUM(sri.quantity) FROM sales_return_items sri JOIN sales_returns sr ON sr.id = sri.sales_return_id WHERE sri.order_item_id = oi.id), 0), o.customer_id
                FROM order_items oi JOIN products p ON p.id = oi.product_id JOIN orders o ON o.id = oi.order_id WHERE oi.order_id = $orderId ORDER BY oi.id;
                """; command.Parameters.AddWithValue("$orderId", orderId); using var reader = command.ExecuteReader(); _selectedCustomerId = null;
            while (reader.Read()) { _selectedCustomerId = reader.IsDBNull(7) ? null : reader.GetInt64(7); _items.Add(new SalesReturnItemDraft { OrderItemId = reader.GetInt64(0), ProductId = reader.GetInt64(1), ProductName = reader.IsDBNull(2) ? "" : reader.GetString(2), SoldQuantity = Convert.ToDecimal(reader.GetValue(3)), UnitPrice = Convert.ToDecimal(reader.GetValue(4)), CostPrice = Convert.ToDecimal(reader.GetValue(5)), AlreadyReturned = Convert.ToDecimal(reader.GetValue(6)), ReturnQuantity = 0 }); }
            RefreshItemsGrid(); MessageBox.Show(this, $"تم تحميل الفاتورة رقم {invoiceNumber}. حدد كميات المرتجع ثم احفظ.", "الفاتورة المحددة", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر تحميل تفاصيل الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void RefreshItemsGrid() { _itemsGrid.Rows.Clear(); foreach (var item in _items) _itemsGrid.Rows.Add(item.ProductName, item.UnitPrice, item.SoldQuantity, item.AlreadyReturned, item.AvailableToReturn, item.ReturnQuantity); UpdateTotal(); }
    private void UpdateTotal() => _total.Text = $"الإجمالي: {_items.Sum(x => x.Total):N2}";

    private void SaveReturn()
    {
        var selected = _items.Where(x => x.ReturnQuantity > 0).ToList(); if (selected.Count == 0) { MessageBox.Show(this, "حدد كمية مرتجع واحدة على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        try { var id = _service.SaveReturn(_selectedOrderId, _selectedCustomerId, _reason.Text, selected); MessageBox.Show(this, $"تم حفظ المرتجع بنجاح.\nرقم العملية: {id}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information); _reason.Clear(); _items.Clear(); _itemsGrid.Rows.Clear(); UpdateTotal(); SearchOrders(); }
        catch (Exception ex) { MessageBox.Show(this, $"تعذر حفظ المرتجع:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ClearSelectedInvoiceOnly() { _selectedOrderId = 0; _selectedCustomerId = null; _items.Clear(); _itemsGrid.Rows.Clear(); _reason.Clear(); UpdateTotal(); }

    private static Button CreateButton(string text, Color color, int width, int height) => new() { Text = text, Width = width, Height = height, BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private sealed record PartyChoice(long Id, string Name);
}
