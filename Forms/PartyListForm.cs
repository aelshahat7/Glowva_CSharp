using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Forms;

public sealed class PartyListForm : Form
{
    private readonly bool _supplierMode;
    private readonly CustomerRepository _customerRepository = new();
    private readonly SupplierRepository _supplierRepository = new();
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = new();

    public PartyListForm(bool supplierMode)
    {
        _supplierMode = supplierMode;
        Text = supplierMode ? "الموردين" : "العملاء";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 650);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(248, 248, 248);
        BuildUi();
        LoadRows();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 68, Padding = new Padding(12) };

        var add = CreateButton(_supplierMode ? "＋ مورد جديد" : "＋ عميل جديد", Color.FromArgb(39, 174, 96));
        add.Dock = DockStyle.Left;
        add.Width = 150;
        add.Click += (_, _) => AddRow();

        var searchButton = CreateButton("بحث", Color.FromArgb(52, 152, 219));
        searchButton.Dock = DockStyle.Right;
        searchButton.Width = 80;
        searchButton.Click += (_, _) => LoadRows();

        _search.Dock = DockStyle.Right;
        _search.Width = 380;
        _search.Font = new Font("Segoe UI", 11);
        _search.PlaceholderText = "ابحث بالاسم أو الكود أو الهاتف...";
        _search.TextAlign = HorizontalAlignment.Right;
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                LoadRows();
                e.SuppressKeyPress = true;
            }
        };

        header.Controls.Add(add);
        header.Controls.Add(searchButton);
        header.Controls.Add(_search);

        ConfigureGrid();
        Controls.Add(_grid);
        Controls.Add(header);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoGenerateColumns = false;
        _grid.RightToLeft = RightToLeft.Yes;
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 42;
        _grid.CellDoubleClick += (_, _) => EditRow();
        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { EditRow(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Delete) { ToggleRow(); e.SuppressKeyPress = true; }
        };

        // The Id is kept as a hidden first column so row actions always use the
        // database Id rather than the visible supplier/customer code (e.g. S00001).
        AddColumn("", "Id", 1);
        _grid.Columns[0].Visible = false;

        AddColumn("الكود", "Code", 100);
        AddColumn("الاسم", "Name", 360, fill: true);
        AddColumn("الهاتف", "Phone", 160);
        AddColumn(_supplierMode ? "بيانات التواصل" : "الهاتف 2", _supplierMode ? "ContactInfo" : "Phone2", 180);
        AddColumn("الرصيد الافتتاحي", "OpeningBalance", 140, "N2");
        AddColumn("الحالة", "IsActive", 100);
    }

    private void AddColumn(string header, string property, int width, string? format = null, bool fill = false)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            Width = width,
            AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        if (format is not null) column.DefaultCellStyle.Format = format;
        column.DefaultCellStyle.Alignment = property == "IsActive"
            ? DataGridViewContentAlignment.MiddleCenter
            : DataGridViewContentAlignment.MiddleRight;
        _grid.Columns.Add(column);
    }

    private void LoadRows()
    {
        try
        {
            if (_supplierMode)
            {
                var rows = _supplierRepository.GetAll(_search.Text);
                _grid.DataSource = rows.Select(x => new
                {
                    x.Id, x.Code, x.Name, x.Phone, x.ContactInfo,
                    x.OpeningBalance,
                    IsActive = x.IsActive ? "نشط" : "موقوف"
                }).ToList();
            }
            else
            {
                var rows = _customerRepository.GetAll(_search.Text);
                _grid.DataSource = rows.Select(x => new
                {
                    x.Id, x.Code, x.Name, x.Phone, x.Phone2,
                    x.OpeningBalance,
                    IsActive = x.IsActive ? "نشط" : "موقوف"
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"تعذر تحميل البيانات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddRow()
    {
        if (_supplierMode)
        {
            var item = new Supplier { Code = _supplierRepository.GenerateNextCode() };
            using var dialog = new SupplierEditorForm(item, true);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { _supplierRepository.Add(dialog.Item); LoadRows(); }
            catch (Exception ex) { ShowSaveError(ex); }
        }
        else
        {
            var item = new Customer { Code = _customerRepository.GenerateNextCode() };
            using var dialog = new CustomerEditorForm(item, true);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { _customerRepository.Add(dialog.Item); LoadRows(); }
            catch (Exception ex) { ShowSaveError(ex); }
        }
    }

    private long? SelectedId()
    {
        if (_grid.CurrentRow is null) return null;
        var value = _grid.CurrentRow.Cells[0].Value;
        return value is null ? null : Convert.ToInt64(value);
    }

    private void EditRow()
    {
        var id = SelectedId();
        if (id is null) return;
        if (_supplierMode)
        {
            var item = _supplierRepository.GetAll().FirstOrDefault(x => x.Id == id.Value);
            if (item is null) return;
            using var dialog = new SupplierEditorForm(item, false);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { _supplierRepository.Update(dialog.Item); LoadRows(); }
            catch (Exception ex) { ShowSaveError(ex); }
        }
        else
        {
            var item = _customerRepository.GetAll().FirstOrDefault(x => x.Id == id.Value);
            if (item is null) return;
            using var dialog = new CustomerEditorForm(item, false);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try { _customerRepository.Update(dialog.Item); LoadRows(); }
            catch (Exception ex) { ShowSaveError(ex); }
        }
    }

    private void ToggleRow()
    {
        var id = SelectedId();
        if (id is null) return;
        if (_supplierMode)
        {
            var item = _supplierRepository.GetAll().FirstOrDefault(x => x.Id == id.Value);
            if (item is null) return;
            var target = !item.IsActive;
            if (MessageBox.Show(this, $"هل تريد {(target ? "تفعيل" : "إيقاف")} المورد «{item.Name}»؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _supplierRepository.SetActive(item.Id, target);
        }
        else
        {
            var item = _customerRepository.GetAll().FirstOrDefault(x => x.Id == id.Value);
            if (item is null) return;
            var target = !item.IsActive;
            if (MessageBox.Show(this, $"هل تريد {(target ? "تفعيل" : "إيقاف")} العميل «{item.Name}»؟", "تأكيد", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _customerRepository.SetActive(item.Id, target);
        }
        LoadRows();
    }

    private void ShowSaveError(Exception ex) => MessageBox.Show(this, $"تعذر حفظ البيانات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static Button CreateButton(string text, Color color) => new()
    {
        Text = text, BackColor = color, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };
}

internal sealed class CustomerEditorForm : PartyEditorBase<Customer>
{
    private readonly TextBox _phone2 = new();
    public Customer Item => Model;
    public CustomerEditorForm(Customer item, bool isNew) : base(item, isNew, "عميل")
    {
        AddTextField("اسم العميل", Model.Name, value => Model.Name = value, required: true);
        AddTextField("الهاتف", Model.Phone, value => Model.Phone = value);
        AddTextBox("الهاتف 2", _phone2); _phone2.Text = Model.Phone2;
        AddTextField("العنوان", Model.Address, value => Model.Address = value);
        AddMoneyField("الرصيد الافتتاحي", Model.OpeningBalance, value => Model.OpeningBalance = value);
        AddNotes(Model.Notes, value => Model.Notes = value);
        SetActive(Model.IsActive);
        Finish();
    }
    protected override void BeforeSave() => Model.Phone2 = _phone2.Text.Trim();
}

internal sealed class SupplierEditorForm : PartyEditorBase<Supplier>
{
    public Supplier Item => Model;
    public SupplierEditorForm(Supplier item, bool isNew) : base(item, isNew, "مورد")
    {
        AddTextField("اسم المورد", Model.Name, value => Model.Name = value, required: true);
        AddTextField("الهاتف", Model.Phone, value => Model.Phone = value);
        AddTextField("بيانات التواصل", Model.ContactInfo, value => Model.ContactInfo = value);
        AddTextField("العنوان", Model.Address, value => Model.Address = value);
        AddMoneyField("الرصيد الافتتاحي", Model.OpeningBalance, value => Model.OpeningBalance = value);
        AddNotes(Model.Notes, value => Model.Notes = value);
        SetActive(Model.IsActive);
        Finish();
    }
}

internal abstract class PartyEditorBase<T> : Form where T : class
{
    protected T Model { get; }
    private readonly TableLayoutPanel _table = new() { ColumnCount = 2, RowCount = 0, Dock = DockStyle.Fill, Padding = new Padding(18), RightToLeft = RightToLeft.Yes };
    private readonly bool _isNew;
    private readonly string _kind;
    private readonly TextBox _code = new();
    private readonly CheckBox _active = new() { Text = "السجل نشط", AutoSize = true };
    private int _row;
    private readonly List<Action> _beforeSave = new();

    protected PartyEditorBase(T model, bool isNew, string kind)
    {
        Model = model; _isNew = isNew; _kind = kind;
        Text = isNew ? $"{kind} جديد" : $"تعديل {kind}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(650, 620);
        RightToLeft = RightToLeft.Yes; RightToLeftLayout = true;
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        AddTextBox("الكود", _code); _code.ReadOnly = true; _code.Text = kind == "عميل" ? ((Customer)(object)model).Code : ((Supplier)(object)model).Code;
        Controls.Add(_table);
    }

    protected void AddTextField(string label, string value, Action<string> setter, bool required = false)
    {
        var box = new TextBox { Dock = DockStyle.Fill, TextAlign = HorizontalAlignment.Right, Text = value };
        box.Tag = new Tuple<Action<string>, bool>(setter, required);
        AddControl(label, box);
        _beforeSave.Add(() => setter(box.Text.Trim()));
    }

    protected void AddTextBox(string label, TextBox box) => AddControl(label, box);

    protected void AddMoneyField(string label, decimal value, Action<decimal> setter)
    {
        var box = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 100000000, Minimum = -100000000, ThousandsSeparator = true, TextAlign = HorizontalAlignment.Right, Value = value };
        AddControl(label, box); _beforeSave.Add(() => setter(box.Value));
    }

    protected void AddNotes(string value, Action<string> setter)
    {
        var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 80, ScrollBars = ScrollBars.Vertical, TextAlign = HorizontalAlignment.Right, Text = value };
        AddControl("ملاحظات", box); _beforeSave.Add(() => setter(box.Text.Trim()));
    }

    protected void SetActive(bool value) { _active.Checked = value; _table.Controls.Add(_active, 0, _row++); _table.SetColumnSpan(_active, 2); }

    private void AddControl(string label, Control control)
    {
        var labelControl = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(4) };
        control.Margin = new Padding(4);
        _table.Controls.Add(labelControl, 0, _row); _table.Controls.Add(control, 1, _row); _row++;
    }

    protected void Finish()
    {
        var save = new Button { Text = _isNew ? $"حفظ {_kind}" : "حفظ التعديلات", Dock = DockStyle.Fill, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        save.Click += (_, _) => Save();
        _table.Controls.Add(save, 0, _row++); _table.SetColumnSpan(save, 2);
        AcceptButton = save;
    }

    protected virtual void BeforeSave() { }

    private void Save()
    {
        try
        {
            foreach (var action in _beforeSave) action();
            BeforeSave();
            if (Model is Customer customer)
            {
                if (string.IsNullOrWhiteSpace(customer.Name)) throw new InvalidOperationException("اسم العميل مطلوب.");
                customer.IsActive = _active.Checked;
            }
            else if (Model is Supplier supplier)
            {
                if (string.IsNullOrWhiteSpace(supplier.Name)) throw new InvalidOperationException("اسم المورد مطلوب.");
                supplier.IsActive = _active.Checked;
            }
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
