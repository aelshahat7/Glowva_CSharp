using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Data;
using GlowvaERP.Helpers;
using GlowvaERP.Models;
using GlowvaERP.Services;

namespace GlowvaERP.Forms;

public sealed class SalesForm : Form
{
    private readonly ComboBox _customer = new();
    private readonly ComboBox _paymentStatus = new();
    private readonly TextBox _productSearch = new();
    private readonly NumericUpDown _quantity = new();
    private readonly NumericUpDown _discount = new();
    private readonly TextBox _notes = new();
    private readonly DataGridView _grid = new();
    private readonly Label _totalLabel = new();
    private readonly Label _cashierLabel = new();
    private readonly Label _profitLabel = new();
    private readonly Label _costLabel = new();
    private readonly Label _invoiceCountLabel = new();
    private readonly SalesOrderService _service = new();
    private readonly List<SalesOrderItemDraft> _items = new();

    public SalesForm()
    {
        Text = "المبيعات";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1200, 760);
        KeyPreview = true;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        BackColor = UiTheme.Page;
        AutoScroll = false;
        BuildUi();
        LoadCustomers();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (KeyboardShortcuts.IsProductSearch(keyData)) { OpenIndependentProductSearch(); return true; }
        if (KeyboardShortcuts.IsProductCard(keyData)) { OpenProductCard(); return true; }
        if (KeyboardShortcuts.IsSave(keyData)) { SaveOrder(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10, 6, 10, 8),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 106F));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));

        main.Controls.Add(InvoiceUi.Title("فاتورة البيع", UiTheme.InvoiceBlue), 0, 0);

        main.Controls.Add(BuildInvoiceHeader(), 0, 1);
        main.Controls.Add(BuildInvoiceSummary(), 0, 2);
        ConfigureGrid();
        main.Controls.Add(_grid, 0, 3);
        main.Controls.Add(BuildBottom(), 0, 4);
        Controls.Add(main);
    }

    private Control BuildInvoiceHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(4, 0, 4, 0),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

        var info = InvoiceUi.FieldRow(8);
        info.Padding = new Padding(0, 2, 0, 2);
        for (var i = 0; i < 8; i++) info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));

        var invoiceType = CreateReadonlyValue("كاش");
        var warehouse = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, RightToLeft = RightToLeft.Yes, Margin = new Padding(2) };
        warehouse.Items.Add("الصيدلية");
        warehouse.SelectedIndex = 0;
        PrepareCombo(_paymentStatus);
        AddFieldPair(info, 0, "نوع", invoiceType);
        AddFieldPair(info, 2, "المخزن", warehouse);
        AddFieldPair(info, 4, "طريقة الدفع", _paymentStatus);
        PrepareCombo(_customer);
        AddFieldPair(info, 6, "العميل", _customer);

        var entry = InvoiceUi.FieldRow(8);
        entry.Padding = new Padding(0, 2, 0, 2);
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
        entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));

        entry.Controls.Add(CreateFieldLabel("الكمية"), 0, 0);
        _quantity.DecimalPlaces = 2;
        _quantity.Minimum = .01M;
        _quantity.Maximum = 1000000M;
        _quantity.Value = 1M;
        _quantity.Dock = DockStyle.Fill;
        _quantity.Margin = new Padding(2);
        _quantity.TextAlign = HorizontalAlignment.Right;
        entry.Controls.Add(_quantity, 1, 0);

        entry.Controls.Add(CreateFieldLabel("الصنف"), 2, 0);
        _productSearch.Dock = DockStyle.Fill;
        _productSearch.Margin = new Padding(2);
        _productSearch.PlaceholderText = "ابحث باسم الصنف أو الكود أو الباركود";
        _productSearch.TextAlign = HorizontalAlignment.Right;
        _productSearch.RightToLeft = RightToLeft.Yes;
        _productSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddFirstSearchResult();
                e.SuppressKeyPress = true;
            }
        };
        entry.Controls.Add(_productSearch, 3, 0);

        entry.Controls.Add(CreateFieldLabel("كود/هاتف"), 4, 0);
        entry.Controls.Add(CreateReadonlyValue("عميل نقدي"), 5, 0);

        var add = InvoiceUi.ActionButton("إضافة الصنف", UiTheme.SecondaryAction);
        add.Dock = DockStyle.Fill;
        add.Margin = new Padding(2);
        add.Click += (_, _) => AddFirstSearchResult();
        entry.Controls.Add(add, 6, 0);

        var search = InvoiceUi.ActionButton("بحث أصناف", UiTheme.Accent);
        search.Dock = DockStyle.Fill;
        search.Margin = new Padding(2);
        search.Click += (_, _) => OpenIndependentProductSearch();
        entry.Controls.Add(search, 7, 0);

        header.Controls.Add(info, 0, 0);
        header.Controls.Add(entry, 0, 1);
        return header;
    }

    private Control BuildInvoiceSummary()
    {
        var summary = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 2,
            Padding = new Padding(0, 2, 0, 2),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(244, 247, 244)
        };
        for (var i = 0; i < 8; i++) summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
        summary.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        summary.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));

        var titles = new[] { "ملاحظات", "إضافية", "م.أصناف", "م.إضافية", "خصم قيمة", "خصم %", "الإجمالي قبل الخصم", "عدد الأصناف" };
        var values = new[] { "", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0" };
        for (var i = 0; i < titles.Length; i++)
        {
            summary.Controls.Add(InvoiceUi.Metric(titles[i], true), i, 0);
            var cell = InvoiceUi.Metric(values[i]);
            if (i == 7)
            {
                cell.Text = _invoiceCountLabel.Text = "0";
            }
            summary.Controls.Add(cell, i, 1);
        }
        return summary;
    }

    private void ConfigureGrid()
    {
        ScrollableLayout.ConfigureGrid(_grid, 34);
        _grid.Dock = DockStyle.Fill;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.BackgroundColor = Color.White;
        UiTheme.StyleInvoiceGrid(_grid, 34);
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && _grid.CurrentRow is not null)
            {
                DeleteSelectedLine();
                e.SuppressKeyPress = true;
            }
        };
        AddGridColumn("الملاحظات", "Note", 7);
        AddGridColumn("موقع الصنف", "Location", 8);
        AddGridColumn("قيمة بعد الخصم", "AfterDiscount", 9);
        AddGridColumn("قيمة الخصم", "DiscountValue", 8);
        AddGridColumn("قيمة الصنف", "UnitPrice", 9);
        AddGridColumn("حد الطلب", "Reorder", 7);
        AddGridColumn("الرصيد", "Balance", 7);
        AddGridColumn("سعر البيع", "UnitPrice", 8);
        AddGridColumn("الكمية", "Quantity", 7);
        AddGridColumn("الوحدة", "Unit", 7);
        AddGridColumn("تاريخ الصلاحية", "Expiry", 10);
        AddGridColumn("اسم الصنف", "ProductName", 18);
        AddGridColumn("كود الصنف", "ProductId", 7);
    }

    private void AddGridColumn(string header, string property, float weight)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        if (property is "Quantity" or "Balance" or "UnitPrice" or "DiscountValue" or "AfterDiscount")
            column.DefaultCellStyle.Format = "N2";
        _grid.Columns.Add(column);
    }

    private Control BuildBottom()
    {
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(6, 4, 6, 2),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(244, 247, 244)
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));

        _totalLabel.Text = "الإجمالي: 0.00";
        _totalLabel.Dock = DockStyle.Fill;
        _totalLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _totalLabel.ForeColor = UiTheme.InvoiceGreen;
        _totalLabel.TextAlign = ContentAlignment.MiddleRight;
        _totalLabel.Padding = new Padding(6);
        bottom.Controls.Add(_totalLabel, 0, 0);

        var discountBox = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, RightToLeft = RightToLeft.Yes, Margin = Padding.Empty, Padding = Padding.Empty };
        discountBox.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        discountBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        discountBox.Controls.Add(CreateFieldLabel("الخصم"), 0, 0);
        _discount.DecimalPlaces = 2;
        _discount.Maximum = 100000000M;
        _discount.Dock = DockStyle.Fill;
        _discount.Margin = new Padding(4);
        UiTheme.StyleInput(_discount);
        _discount.TextAlign = HorizontalAlignment.Right;
        _discount.ValueChanged += (_, _) => UpdateTotal();
        discountBox.Controls.Add(_discount, 1, 0);
        bottom.Controls.Add(discountBox, 1, 0);

        _costLabel.Text = "تكلفة الفاتورة: 0.00";
        _costLabel.Dock = DockStyle.Fill;
        _costLabel.TextAlign = ContentAlignment.MiddleRight;
        _costLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        bottom.Controls.Add(_costLabel, 2, 0);

        _profitLabel.Text = "قيمة ربح الفاتورة: 0.00";
        _profitLabel.Dock = DockStyle.Fill;
        _profitLabel.TextAlign = ContentAlignment.MiddleRight;
        _profitLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _profitLabel.ForeColor = Color.FromArgb(0, 120, 0);
        bottom.Controls.Add(_profitLabel, 3, 0);

        _cashierLabel.Text = "المستخدم الحالي: Administrator";
        _cashierLabel.Dock = DockStyle.Fill;
        _cashierLabel.TextAlign = ContentAlignment.MiddleRight;
        _cashierLabel.Font = new Font("Segoe UI", 8.5F);
        bottom.Controls.Add(_cashierLabel, 0, 1);

        _notes.Multiline = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Dock = DockStyle.Fill;
        _notes.Margin = new Padding(4);
        UiTheme.StyleInput(_notes);
        _notes.TextAlign = HorizontalAlignment.Right;
        _notes.RightToLeft = RightToLeft.Yes;
        _notes.PlaceholderText = "ملاحظات";
        bottom.Controls.Add(_notes, 1, 1);
        bottom.SetColumnSpan(_notes, 2);

        var save = InvoiceUi.ActionButton("حفظ فاتورة البيع", UiTheme.PrimaryAction);
        save.Dock = DockStyle.Fill;
        save.Margin = new Padding(4);
        save.Click += (_, _) => SaveOrder();
        bottom.Controls.Add(save, 3, 1);
        return bottom;
    }

    private static void AddFieldPair(TableLayoutPanel parent, int startColumn, string labelText, Control value)
    {
        parent.Controls.Add(CreateFieldLabel(labelText), startColumn, 0);
        parent.Controls.Add(value, startColumn + 1, 0);
        parent.SetColumnSpan(value, 1);
    }

    private static TextBox CreateReadonlyValue(string text)
    {
        var value = new TextBox
        {
            Text = text,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(2),
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        UiTheme.StyleInput(value);
        return value;
    }

    private static void PrepareCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.RightToLeft = RightToLeft.Yes;
        combo.Margin = new Padding(2);
        UiTheme.StyleInput(combo);
    }

    private static Label CreateFieldLabel(string text) => InvoiceUi.FieldLabel(text);

    private void OpenIndependentProductSearch()
    {
        using var dialog = new ProductSearchDialog();
        dialog.ShowDialog(this);
    }

    private void OpenProductCard()
    {
        long? productId = null;
        if (_grid.CurrentRow?.DataBoundItem is not null)
        {
            var value = _grid.CurrentRow.Cells[0].Value;
            if (long.TryParse(Convert.ToString(value), out var id)) productId = id;
        }

        if (!productId.HasValue)
        {
            var query = _productSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(query))
            {
                using var connection = Database.OpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT id FROM products WHERE is_active = 1 AND (name = $exact OR code = $exact OR barcode = $exact OR name LIKE $like OR code LIKE $like OR barcode LIKE $like) ORDER BY CASE WHEN name = $exact THEN 0 WHEN code = $exact THEN 1 WHEN barcode = $exact THEN 2 ELSE 3 END, id LIMIT 1;";
                command.Parameters.AddWithValue("$exact", query);
                command.Parameters.AddWithValue("$like", $"%{query}%");
                var result = command.ExecuteScalar();
                if (result is not null) productId = Convert.ToInt64(result);
            }
        }

        if (!productId.HasValue)
        {
            MessageBox.Show(this, "حدد صنفًا أو اكتب اسم الصنف أولًا.", "بيانات الصنف", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var card = new ProductCardDialog(productId.Value);
        card.ShowDialog(this);
    }

    private void LoadCustomers()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, code, name FROM customers WHERE is_active = 1 ORDER BY name;";
            var list = new List<CustomerChoice> { new(0, "عميل نقدي", "") };
            using var reader = command.ExecuteReader();
            while (reader.Read()) list.Add(new CustomerChoice(reader.GetInt64(0), reader.IsDBNull(2) ? "" : reader.GetString(2), reader.IsDBNull(1) ? "" : reader.GetString(1)));
            _customer.DataSource = list;
            _customer.DisplayMember = nameof(CustomerChoice.DisplayName);
            _customer.ValueMember = nameof(CustomerChoice.Id);
            _paymentStatus.Items.Clear();
            _paymentStatus.Items.AddRange(new object[] { "كاش", "فيزا", "آجل", "محفظة إلكترونية", "تحويل بنكي" });
            _paymentStatus.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل العملاء:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddFirstSearchResult()
    {
        var query = _productSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, sell_price, buy_price FROM products WHERE is_active = 1 AND (name LIKE $q OR code LIKE $q OR barcode LIKE $q) ORDER BY CASE WHEN barcode = $exact THEN 0 WHEN code = $exact THEN 1 WHEN name = $exact THEN 2 ELSE 3 END, id LIMIT 1;";
            command.Parameters.AddWithValue("$q", $"%{query}%");
            command.Parameters.AddWithValue("$exact", query);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                MessageBox.Show(this, "الصنف غير موجود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = reader.GetInt64(0);
            var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var sell = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2));
            var buy = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3));
            var existing = _items.FirstOrDefault(x => x.ProductId == id);
            if (existing is not null) existing.Quantity += _quantity.Value;
            else _items.Add(new SalesOrderItemDraft { ProductId = id, ProductName = name, Quantity = _quantity.Value, UnitPrice = sell, CostPrice = buy });
            _productSearch.Clear();
            _quantity.Value = 1M;
            RefreshGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر إضافة الصنف:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshGrid()
    {
        _grid.DataSource = null;
        _grid.DataSource = _items.Select(x => new
        {
            x.ProductId,
            x.ProductName,
            Expiry = "",
            Unit = "علبة",
            x.Quantity,
            Balance = 0m,
            x.UnitPrice,
            Reorder = 0m,
            DiscountValue = 0m,
            AfterDiscount = x.Total,
            Location = "أماكن الأصناف",
            Note = ""
        }).ToList();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        var subtotal = _items.Sum(x => x.Total);
        var total = Math.Max(0m, subtotal - _discount.Value);
        var cost = _items.Sum(x => x.CostPrice * x.Quantity);
        var profit = total - cost;
        _totalLabel.Text = $"الإجمالي: {total:N2}";
        _costLabel.Text = $"تكلفة الفاتورة: {cost:N2}";
        _profitLabel.Text = $"قيمة ربح الفاتورة: {profit:N2}";
        _invoiceCountLabel.Text = _items.Count.ToString();
    }

    private void SaveOrder()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show(this, "أضف صنفًا واحدًا على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var customerId = Convert.ToInt64(_customer.SelectedValue ?? 0);
        if (_paymentStatus.Text == "آجل" && customerId == 0)
        {
            MessageBox.Show(this, "لا يمكن حفظ فاتورة آجلة بدون اختيار عميل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var orderId = _service.SaveOrder(customerId == 0 ? null : customerId, _paymentStatus.Text, _discount.Value, _notes.Text, _items);
            MessageBox.Show(this, $"تم حفظ الفاتورة بنجاح.\nرقم العملية: {orderId}", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCurrentInvoice();
            _productSearch.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر حفظ الفاتورة:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void TriggerSave() => SaveOrder();
    public void FocusProductEntry() => _productSearch.Focus();
    public void AddLine() => AddFirstSearchResult();

    public void DeleteSelectedLine()
    {
        if (_grid.CurrentRow is null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _items.Count) return;
        _items.RemoveAt(_grid.CurrentRow.Index);
        RefreshGrid();
    }

    public void ClearCurrentInvoice()
    {
        _items.Clear();
        _discount.Value = 0M;
        _notes.Clear();
        _productSearch.Clear();
        _quantity.Value = 1M;
        RefreshGrid();
    }

    private sealed record CustomerChoice(long Id, string Name, string Code = "")
    {
        public string DisplayName => Id == 0 ? Name : $"{Name} - {Code}";
    }
}
