using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Data;
using GlowvaERP.Helpers;
using GlowvaERP.Models;
using GlowvaERP.Services;
using ServicePaymentMethods = GlowvaERP.Services.PaymentMethods;

namespace GlowvaERP.Forms;

public sealed class PurchasesForm : Form
{
    private readonly ComboBox _supplier = new();
    private readonly ComboBox _paymentStatus = new();
    private readonly TextBox _supplierInvoiceNumber = new();
    private readonly TextBox _productSearch = new();
    private readonly NumericUpDown _quantity = new();
    private readonly NumericUpDown _discount = new();
    private readonly TextBox _notes = new();
    private readonly DataGridView _grid = new();
    private readonly Label _totalLabel = new();
    private readonly PurchaseOrderService _service = new();
    private readonly List<PurchaseOrderItemDraft> _items = new();

    private static string A(string value) => value;

    public PurchasesForm()
    {
        Text = A("\u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a");
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1050, 680);
        KeyPreview = true;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = UiTheme.Page;
        BuildUi();
        LoadSuppliers();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (KeyboardShortcuts.IsProductSearch(keyData)) { OpenProductSearch(); return true; }
        if (KeyboardShortcuts.IsSave(keyData)) { SavePurchase(); return true; }
        if (KeyboardShortcuts.IsCancel(keyData)) { ClearCurrentPurchase(); return true; }
        if (KeyboardShortcuts.IsDeleteRow(keyData)) { DeleteSelectedLine(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = UiTheme.Page,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        root.Controls.Add(BuildContent(), 0, 0);
        root.Controls.Add(BuildSidebar(), 1, 0);
        Controls.Add(root);
    }

    private Control BuildSidebar()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4) };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            RightToLeft = RightToLeft.Yes,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };
        AddSideButton(flow, "\ud83d\udcbE  \u062d\u0641\u0638", UiTheme.PrimaryAction, SavePurchase);
        AddSideButton(flow, "\ud83d\udd04  \u0645\u0631\u0627\u062c\u0639\u0629 \u0627\u0644\u0641\u0648\u0627\u062a\u064a\u0631", UiTheme.Accent, OpenLastInvoices);
        AddSideButton(flow, "\u2795  \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641", UiTheme.SecondaryAction, AddFirstSearchResult);
        AddSideButton(flow, "\u274c  \u062d\u0630\u0641 \u0633\u0637\u0631", UiTheme.DangerAction, DeleteSelectedLine);
        AddSideButton(flow, "\u21ba  \u0625\u0644\u063a\u0627\u0621", Color.FromArgb(90, 90, 90), ClearCurrentPurchase);
        panel.Controls.Add(flow);
        return panel;
    }

    private static void AddSideButton(FlowLayoutPanel host, string text, Color color, Action action)
    {
        var button = InvoiceUi.ActionButton(text, color, 42);
        button.Width = 116;
        button.Height = 52;
        button.Margin = new Padding(2, 2, 2, 4);
        button.Click += (_, _) => action();
        host.Controls.Add(button);
    }

    private Control BuildContent()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            RightToLeft = RightToLeft.Yes,
            BackColor = UiTheme.Page,
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        content.Controls.Add(BuildTitle(), 0, 0);
        content.Controls.Add(BuildInvoiceHeader(), 0, 1);
        content.Controls.Add(BuildItemsArea(), 0, 2);
        content.Controls.Add(BuildFooter(), 0, 3);
        return content;
    }

    private Control BuildTitle()
    {
        var label = new Label
        {
            Text = A("\u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0645\u0634\u062a\u0631\u064a\u0627\u062a"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = UiTheme.InvoiceGreen,
            BackColor = Color.White,
        };
        return label;
    }

    private Control BuildInvoiceHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 2,
            Padding = new Padding(8, 5, 8, 5),
            Margin = Padding.Empty,
            BackColor = Color.White,
            RightToLeft = RightToLeft.Yes,
        };
        for (int i = 0; i < 6; i++) header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, i % 2 == 0 ? 10 : 23.33f));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        PrepareCombo(_supplier);
        PrepareCombo(_paymentStatus);
        _paymentStatus.Items.AddRange(new object[] { A("\u0646\u0642\u062f\u064a"), A("\u0622\u062c\u0644") });
        _paymentStatus.SelectedIndex = 0;
        PrepareInput(_supplierInvoiceNumber);
        _supplierInvoiceNumber.PlaceholderText = A("\u0631\u0642\u0645 \u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0645\u0648\u0631\u062f");

        AddPair(header, 0, A("\u0627\u0644\u0645\u0648\u0631\u062f"), _supplier, 0);
        AddPair(header, 2, A("\u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062f\u0641\u0639"), _paymentStatus, 0);
        AddPair(header, 4, A("\u0631\u0642\u0645 \u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0645\u0648\u0631\u062f"), _supplierInvoiceNumber, 0);

        var warehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        PrepareCombo(warehouse);
        warehouse.Items.Add(A("\u0627\u0644\u0635\u064a\u062f\u0644\u064a\u0629"));
        warehouse.SelectedIndex = 0;
        AddPair(header, 0, A("\u0627\u0644\u0645\u062e\u0632\u0646"), warehouse, 1);

        var dateBox = new TextBox { ReadOnly = true, Text = DateTime.Now.ToString("yyyy-MM-dd"), TextAlign = HorizontalAlignment.Right };
        PrepareInput(dateBox);
        AddPair(header, 2, A("\u0627\u0644\u062a\u0627\u0631\u064a\u062e"), dateBox, 1);

        var status = new Label
        {
            Text = A("\u062d\u0627\u0644\u0629 \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629: \u062c\u062f\u064a\u062f\u0629"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = UiTheme.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
        };
        header.Controls.Add(status, 4, 1);
        header.SetColumnSpan(status, 2);
        return header;
    }

    private static void AddPair(TableLayoutPanel host, int column, string label, Control input, int row)
    {
        host.Controls.Add(InvoiceUi.FieldLabel(label), column, row);
        host.Controls.Add(input, column + 1, row);
    }

    private Control BuildItemsArea()
    {
        var area = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 4, 0, 0),
            Margin = Padding.Empty,
            BackColor = Color.White,
            RightToLeft = RightToLeft.Yes,
        };
        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        area.Controls.Add(BuildProductEntry(), 0, 0);
        ConfigureGrid();
        area.Controls.Add(_grid, 0, 1);
        return area;
    }

    private Control BuildProductEntry()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(6, 4, 6, 4),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        _quantity.DecimalPlaces = 2;
        _quantity.Minimum = .01M;
        _quantity.Maximum = 1000000M;
        _quantity.Value = 1M;
        PrepareInput(_quantity);
        PrepareInput(_productSearch);
        _productSearch.PlaceholderText = A("\u0627\u0628\u062d\u062b \u0628\u0627\u0633\u0645 \u0627\u0644\u0635\u0646\u0641 \u0623\u0648 \u0627\u0644\u0643\u0648\u062f \u0623\u0648 \u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f");
        _productSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { AddFirstSearchResult(); e.SuppressKeyPress = true; } };

        row.Controls.Add(InvoiceUi.FieldLabel(A("\u0627\u0644\u0643\u0645\u064a\u0629")), 0, 0);
        row.Controls.Add(_quantity, 1, 0);
        row.Controls.Add(InvoiceUi.FieldLabel(A("\u0627\u0644\u0635\u0646\u0641")), 2, 0);
        row.Controls.Add(_productSearch, 3, 0);

        var add = InvoiceUi.ActionButton(A("\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0635\u0646\u0641"), UiTheme.SecondaryAction);
        add.Dock = DockStyle.Fill;
        add.Margin = new Padding(2);
        add.Click += (_, _) => AddFirstSearchResult();
        row.Controls.Add(add, 4, 0);

        var search = InvoiceUi.ActionButton(A("\u0628\u062d\u062b \u0627\u0644\u0623\u0635\u0646\u0627\u0641"), UiTheme.Accent);
        search.Dock = DockStyle.Fill;
        search.Margin = new Padding(2);
        search.Click += (_, _) => OpenProductSearch();
        row.Controls.Add(search, 5, 0);
        return row;
    }

    private void ConfigureGrid()
    {
        ScrollableLayout.ConfigureGrid(_grid, 34);
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        UiTheme.StyleInvoiceGrid(_grid, 34);
        AddColumn(A("\u0627\u0644\u0635\u0646\u0641"), "ProductName", 40);
        AddColumn(A("\u0627\u0644\u0643\u0645\u064a\u0629"), "Quantity", 18, "N2");
        AddColumn(A("\u0633\u0639\u0631 \u0627\u0644\u0634\u0631\u0627\u0621"), "UnitPrice", 20, "N2");
        AddColumn(A("\u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a"), "Total", 22, "N2");
        _grid.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) { DeleteSelectedLine(); e.SuppressKeyPress = true; } };
    }

    private void AddColumn(string header, string property, float weight, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
        };
        if (format is not null) column.DefaultCellStyle.Format = format;
        _grid.Columns.Add(column);
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(6),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(244, 247, 244),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        _totalLabel.Dock = DockStyle.Fill;
        _totalLabel.Text = A("\u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a: 0.00");
        _totalLabel.TextAlign = ContentAlignment.MiddleRight;
        _totalLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        _totalLabel.ForeColor = UiTheme.InvoiceGreen;
        footer.Controls.Add(_totalLabel, 0, 0);

        var discountRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RightToLeft = RightToLeft.Yes };
        discountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        discountRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        discountRow.Controls.Add(InvoiceUi.FieldLabel(A("\u0627\u0644\u062e\u0635\u0645")), 0, 0);
        _discount.DecimalPlaces = 2;
        _discount.Maximum = 100000000M;
        PrepareInput(_discount);
        _discount.ValueChanged += (_, _) => UpdateTotal();
        discountRow.Controls.Add(_discount, 1, 0);
        footer.Controls.Add(discountRow, 1, 0);

        var count = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = UiTheme.Muted, Text = A("\u0639\u062f\u062f \u0627\u0644\u0623\u0635\u0646\u0627\u0641: 0") };
        footer.Controls.Add(count, 2, 0);

        var save = InvoiceUi.ActionButton(A("\u062d\u0641\u0638 \u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0634\u0631\u0627\u0621"), UiTheme.PrimaryAction, 42);
        save.Dock = DockStyle.Fill;
        save.Click += (_, _) => SavePurchase();
        footer.Controls.Add(save, 3, 0);

        _notes.Multiline = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.PlaceholderText = A("\u0645\u0644\u0627\u062d\u0638\u0627\u062a");
        PrepareInput(_notes);
        footer.Controls.Add(_notes, 0, 1);
        footer.SetColumnSpan(_notes, 3);

        var hint = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = UiTheme.Muted, Font = new Font("Segoe UI", 8.5F), Text = A("\u064a\u062a\u0645 \u062a\u062d\u062f\u064a\u062b \u0627\u0644\u0645\u062e\u0632\u0648\u0646 \u0639\u0646\u062f \u062d\u0641\u0638 \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629") };
        footer.Controls.Add(hint, 3, 1);
        return footer;
    }

    private void LoadSuppliers()
    {
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, code, name FROM suppliers WHERE is_active = 1 ORDER BY name;";
            var list = new List<SupplierChoice> { new(0, A("\u0645\u0648\u0631\u062f \u0646\u0642\u062f\u064a"), "") };
            using var reader = command.ExecuteReader();
            while (reader.Read())
                list.Add(new SupplierChoice(reader.GetInt64(0), reader.IsDBNull(2) ? "" : reader.GetString(2), reader.IsDBNull(1) ? "" : reader.GetString(1)));
            _supplier.DataSource = list;
            _supplier.DisplayMember = nameof(SupplierChoice.DisplayName);
            _supplier.ValueMember = nameof(SupplierChoice.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, A("\u062a\u0639\u0630\u0631 \u062a\u062d\u0645\u064a\u0644 \u0627\u0644\u0645\u0648\u0631\u062f\u064a\u0646") + "\n" + ex.Message, A("\u062e\u0637\u0623"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenProductSearch()
    {
        using var dialog = new ProductSearchDialog(_productSearch.Text.Trim());
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedProductName))
        {
            _productSearch.Text = dialog.SelectedProductName;
            _productSearch.Focus();
        }
    }

    private static void OpenLastInvoices()
    {
        using var dialog = new InvoiceSearchForm(false);
        dialog.ShowDialog();
    }

    private void AddFirstSearchResult()
    {
        var query = _productSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;
        try
        {
            using var connection = Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, buy_price FROM products WHERE is_active = 1 AND (name LIKE $q OR code LIKE $q OR barcode LIKE $q) ORDER BY CASE WHEN barcode = $exact THEN 0 WHEN code = $exact THEN 1 WHEN name = $exact THEN 2 ELSE 3 END, id LIMIT 1;";
            command.Parameters.AddWithValue("$q", $"%{query}%");
            command.Parameters.AddWithValue("$exact", query);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                MessageBox.Show(this, A("\u0627\u0644\u0635\u0646\u0641 \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f"), A("\u062a\u0646\u0628\u064a\u0647"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var id = reader.GetInt64(0);
            var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var buy = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2));
            var existing = _items.FirstOrDefault(x => x.ProductId == id);
            if (existing is not null) existing.Quantity += _quantity.Value;
            else _items.Add(new PurchaseOrderItemDraft { ProductId = id, ProductName = name, Quantity = _quantity.Value, UnitPrice = buy });
            _productSearch.Clear();
            _quantity.Value = 1M;
            RefreshGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, A("\u062a\u0639\u0630\u0631 \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u0635\u0646\u0641") + "\n" + ex.Message, A("\u062e\u0637\u0623"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshGrid()
    {
        _grid.DataSource = null;
        _grid.DataSource = _items.Select(x => new { x.ProductName, x.Quantity, x.UnitPrice, x.Total }).ToList();
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        var subtotal = _items.Sum(x => x.Total);
        _totalLabel.Text = $"{A("\u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a")}: {Math.Max(0m, subtotal - _discount.Value):N2}";
    }

    private void SavePurchase()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show(this, A("\u0623\u0636\u0641 \u0635\u0646\u0641\u064b\u0627 \u0648\u0627\u062d\u062f\u064b\u0627 \u0639\u0644\u0649 \u0627\u0644\u0623\u0642\u0644"), A("\u062a\u0646\u0628\u064a\u0647"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var supplierId = Convert.ToInt64(_supplier.SelectedValue ?? 0);
        if (_paymentStatus.Text == ServicePaymentMethods.Credit && supplierId == 0)
        {
            MessageBox.Show(this, A("\u0644\u0627 \u064a\u0645\u0643\u0646 \u062d\u0641\u0638 \u0641\u0627\u062a\u0648\u0631\u0629 \u0634\u0631\u0627\u0621 \u0622\u062c\u0644\u0629 \u0628\u062f\u0648\u0646 \u0645\u0648\u0631\u062f"), A("\u062a\u0646\u0628\u064a\u0647"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var purchaseId = _service.SavePurchase(
                supplierId == 0 ? null : supplierId,
                _paymentStatus.Text,
                _supplierInvoiceNumber.Text.Trim(),
                _discount.Value,
                _notes.Text,
                _items);
            MessageBox.Show(this, A("\u062a\u0645 \u062d\u0641\u0638 \u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0634\u0631\u0627\u0621 \u0628\u0646\u062c\u0627\u062d") + "\n" + A("\u0631\u0642\u0645 \u0627\u0644\u0639\u0645\u0644\u064a\u0629") + $": {purchaseId}", A("\u062a\u0645 \u0627\u0644\u062d\u0641\u0638"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearCurrentPurchase();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, A("\u062a\u0639\u0630\u0631 \u062d\u0641\u0638 \u0641\u0627\u062a\u0648\u0631\u0629 \u0627\u0644\u0634\u0631\u0627\u0621") + "\n" + ex.Message, A("\u062e\u0637\u0623"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void TriggerSave() => SavePurchase();
    public void FocusProductEntry() => _productSearch.Focus();
    public void AddLine() => AddFirstSearchResult();

    public void DeleteSelectedLine()
    {
        if (_grid.CurrentRow is null || _grid.CurrentRow.Index < 0 || _grid.CurrentRow.Index >= _items.Count) return;
        _items.RemoveAt(_grid.CurrentRow.Index);
        RefreshGrid();
    }

    public void ClearCurrentPurchase()
    {
        _items.Clear();
        _discount.Value = 0M;
        _supplierInvoiceNumber.Clear();
        _notes.Clear();
        _productSearch.Clear();
        _quantity.Value = 1M;
        RefreshGrid();
    }

    private static void PrepareCombo(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.RightToLeft = RightToLeft.Yes;
        combo.Margin = new Padding(2);
        UiTheme.StyleInput(combo);
    }

    private static void PrepareInput(Control control)
    {
        control.Dock = DockStyle.Fill;
        control.RightToLeft = RightToLeft.Yes;
        control.Margin = new Padding(2);
        UiTheme.StyleInput(control);
    }

    private sealed record SupplierChoice(long Id, string Name, string Code = "")
    {
        public string DisplayName => Id == 0 ? Name : $"{Name} - {Code}";
    }
}
