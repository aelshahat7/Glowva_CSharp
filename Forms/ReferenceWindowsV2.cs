using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using GlowvaERP.Services;

namespace GlowvaERP.Forms;

internal static class RefV2
{
    public static readonly Color Gold = Color.FromArgb(255, 204, 74);
    public static readonly Color Blue = Color.FromArgb(0, 100, 210);
    public static readonly Color Gray = Color.FromArgb(238, 238, 238);
    public static readonly Color Cream = Color.FromArgb(255, 248, 232);
    public static readonly Color Green = Color.FromArgb(205, 255, 210);
    public static readonly Color Cyan = Color.FromArgb(220, 250, 252);
    public static readonly Color Lavender = Color.FromArgb(238, 232, 255);
    public static readonly Color Warning = Color.FromArgb(255, 248, 190);
    public static readonly Font Normal = new("Tahoma", 9F);
    public static readonly Font Bold = new("Tahoma", 9F, FontStyle.Bold);
    public static readonly Font Title = new("Tahoma", 20F, FontStyle.Bold);

    public static void Prepare(Form form, Size size, bool resize = false)
    {
        form.ClientSize = size;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = true;
        form.Font = Normal;
        form.BackColor = Color.FromArgb(240, 240, 240);
        form.FormBorderStyle = resize ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;
        form.MaximizeBox = resize;
        form.MinimizeBox = false;
        form.ShowInTaskbar = false;
    }

    public static Label TitleLabel(string text, int height = 58)
        => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = height,
            Font = Title,
            ForeColor = Blue,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(245, 245, 245),
            BorderStyle = BorderStyle.FixedSingle
        };

    public static Button Button(string text, int width = 92)
        => new()
        {
            Text = text,
            Width = width,
            Height = 30,
            Font = Bold,
            FlatStyle = FlatStyle.Standard,
            RightToLeft = RightToLeft.Yes,
            UseVisualStyleBackColor = true,
            Margin = new Padding(4)
        };

    public static TextBox TextBox(string text = "")
        => new()
        {
            Text = text,
            Height = 25,
            Font = Normal,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            RightToLeft = RightToLeft.Yes
        };

    public static ComboBox Combo(IEnumerable<string> values, string? selected = null)
    {
        var combo = new ComboBox
        {
            Height = 25,
            Font = Normal,
            DropDownStyle = ComboBoxStyle.DropDownList,
            RightToLeft = RightToLeft.Yes
        };
        var list = values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        combo.Items.AddRange(list);
        if (selected != null)
        {
            var index = combo.Items.IndexOf(selected);
            combo.SelectedIndex = index >= 0 ? index : (combo.Items.Count > 0 ? 0 : -1);
        }
        else if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
        return combo;
    }

    public static ComboBox InstalledPrinters(string? selected = null)
        => Combo(PrinterSettings.InstalledPrinters.Cast<string>(), selected);

    public static CheckBox Check(string text, bool value = false)
        => new()
        {
            Text = text,
            Checked = value,
            AutoSize = true,
            Font = Normal,
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleRight
        };

    public static NumericUpDown Numeric(decimal value, decimal min = 0, decimal max = 9999)
        => new()
        {
            Value = Math.Min(Math.Max(value, min), max),
            Minimum = min,
            Maximum = max,
            Width = 70,
            Height = 25,
            Font = Bold,
            TextAlign = HorizontalAlignment.Right
        };

    public static Label L(string text, int width = 150)
        => new()
        {
            Text = text,
            Width = width,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Font = Bold,
            RightToLeft = RightToLeft.Yes
        };

    public static FlowLayoutPanel Footer(params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(238, 238, 238),
            Padding = new Padding(8, 7, 8, 7)
        };
        foreach (var button in buttons) panel.Controls.Add(button);
        return panel;
    }

    public static Panel Section(string title, Color color, int height)
    {
        var box = new Panel
        {
            Width = 452,
            Height = height,
            BackColor = color,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8)
        };
        box.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = Bold,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        });
        return box;
    }

    public static string Setting(AppSettingsService service, string key, string fallback = "")
        => service.Get(key, fallback) ?? fallback;
}

public abstract class ReferenceV2Base : Form
{
    protected readonly AppSettingsService Settings = new();

    protected ReferenceV2Base(string title, Size size, bool resize = false)
    {
        Text = title;
        RefV2.Prepare(this, size, resize);
        FormClosed += (_, _) => DisposeChildResources();
    }

    protected void AddTitle(string title) => Controls.Add(RefV2.TitleLabel(title));

    protected virtual void DisposeChildResources() { }

    protected static Label Pair(Control panel, string text, Control control, int x, int y, int controlWidth = 270)
    {
        control.SetBounds(x, y, controlWidth, control.Height);
        panel.Controls.Add(control);
        var label = RefV2.L(text);
        label.SetBounds(x + controlWidth + 8, y, 150, 27);
        panel.Controls.Add(label);
        return label;
    }
}

public sealed class OrganizationDataV2Form : ReferenceV2Base
{
    private readonly TextBox _arabic = RefV2.TextBox();
    private readonly TextBox _english = RefV2.TextBox();
    private readonly TextBox _commercial = RefV2.TextBox();
    private readonly TextBox _tax = RefV2.TextBox();
    private readonly TextBox _owner = RefV2.TextBox();
    private readonly TextBox _manager = RefV2.TextBox();
    private readonly TextBox _phone = RefV2.TextBox();
    private readonly TextBox _fax = RefV2.TextBox();
    private readonly TextBox _address = RefV2.TextBox();
    private readonly PictureBox _logo = new() { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

    public OrganizationDataV2Form() : base("بيانات المؤسسة", new Size(930, 620))
    {
        AddTitle("بيانات المؤسسة");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.FromArgb(240, 240, 240) };
        var fields = new TableLayoutPanel { Location = new Point(8, 72), Size = new Size(890, 215), ColumnCount = 4, RowCount = 4, RightToLeft = RightToLeft.Yes };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var i = 0; i < 4; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        AddField(fields, "الكود", RefV2.TextBox("1"), 0, 0);
        AddField(fields, "الاسم العربي", _arabic, 1, 0);
        AddField(fields, "الاسم الإنجليزي", _english, 2, 0, 2);
        AddField(fields, "رقم السجل التجاري", _commercial, 0, 1, 2);
        AddField(fields, "رقم البطاقة الضريبية", _tax, 2, 1);
        AddField(fields, "صاحب المؤسسة", _owner, 0, 2, 2);
        AddField(fields, "المدير المسئول", _manager, 2, 2);
        AddField(fields, "التليفون", _phone, 0, 3);
        AddField(fields, "الفاكس", _fax, 1, 3);
        AddField(fields, "العنوان", _address, 2, 3, 2);

        _logo.SetBounds(35, 315, 170, 105);
        var choose = RefV2.Button("اختيار الصورة", 105); choose.SetBounds(64, 430, 105, 30); choose.Click += ChooseImage;
        body.Controls.Add(fields); body.Controls.Add(_logo); body.Controls.Add(choose);

        LoadSettings();
        var save = RefV2.Button("حفظ"); save.Click += SaveSettings;
        var close = RefV2.Button("إغلاق"); close.Click += (_, _) => Close();
        Controls.Add(body); Controls.Add(RefV2.Footer(close, save));
    }

    private static void AddField(TableLayoutPanel p, string label, Control control, int col, int row, int span = 1)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        var l = RefV2.L(label, 120); l.Dock = DockStyle.Right;
        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control); panel.Controls.Add(l);
        p.Controls.Add(panel, col, row);
        if (span > 1) p.SetColumnSpan(panel, span);
    }

    private void LoadSettings()
    {
        _arabic.Text = RefV2.Setting(Settings, "org.name_ar", "الصيدلية");
        _english.Text = RefV2.Setting(Settings, "org.name_en");
        _commercial.Text = RefV2.Setting(Settings, "org.commercial");
        _tax.Text = RefV2.Setting(Settings, "org.tax");
        _owner.Text = RefV2.Setting(Settings, "org.owner");
        _manager.Text = RefV2.Setting(Settings, "org.manager");
        _phone.Text = RefV2.Setting(Settings, "org.phone");
        _fax.Text = RefV2.Setting(Settings, "org.fax");
        _address.Text = RefV2.Setting(Settings, "org.address");
        var b64 = RefV2.Setting(Settings, "org.logo");
        if (!string.IsNullOrWhiteSpace(b64))
        {
            try { using var ms = new MemoryStream(Convert.FromBase64String(b64)); _logo.Image = Image.FromStream(ms); } catch { }
        }
    }

    private void ChooseImage(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        using var temp = Image.FromFile(dialog.FileName);
        _logo.Image = new Bitmap(temp);
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        Settings.Set("org.name_ar", _arabic.Text.Trim());
        Settings.Set("org.name_en", _english.Text.Trim());
        Settings.Set("org.commercial", _commercial.Text.Trim());
        Settings.Set("org.tax", _tax.Text.Trim());
        Settings.Set("org.owner", _owner.Text.Trim());
        Settings.Set("org.manager", _manager.Text.Trim());
        Settings.Set("org.phone", _phone.Text.Trim());
        Settings.Set("org.fax", _fax.Text.Trim());
        Settings.Set("org.address", _address.Text.Trim());
        if (_logo.Image != null)
        {
            using var ms = new MemoryStream();
            _logo.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            Settings.Set("org.logo", Convert.ToBase64String(ms.ToArray()));
        }
        MessageBox.Show(this, "تم حفظ بيانات المؤسسة.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public sealed class OperatingSettingsV2Form : ReferenceV2Base
{
    private readonly Panel _scroll = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(245, 245, 245) };
    private readonly ComboBox _purchaseWarehouse = RefV2.Combo(new[] { "الصيدلية" });
    private readonly ComboBox _salesWarehouse = RefV2.Combo(new[] { "الصيدلية" });
    private readonly NumericUpDown _returnDays = RefV2.Numeric(14, 0, 365);
    private readonly ComboBox _cashier = RefV2.Combo(new[] { "نقطة بيع 1" });
    private readonly ComboBox _cashSupply = RefV2.Combo(new[] { "نقطة بيع 1" });
    private readonly ComboBox _cashExpense = RefV2.Combo(new[] { "نقطة بيع 1" });
    private readonly ComboBox _outgoingChecks = RefV2.Combo(new[] { "حساب بنك مصر" });
    private readonly CheckBox _allowExpiredPurchase = RefV2.Check("السماح بتسجيل أصناف منتهية الصلاحية في المشتريات");
    private readonly CheckBox _changePurchaseWarehouse = RefV2.Check("إمكانية تغيير مخزن المشتريات مع كل فاتورة");
    private readonly CheckBox _paidMessage = RefV2.Check("ظهور رسالة المدفوع والمتبقي مع حفظ شاشة البيع", true);
    private readonly CheckBox _expiryWarning = RefV2.Check("ظهور رسالة تحذير تواريخ الصلاحية الأقرب", true);
    private readonly CheckBox _preventExpired = RefV2.Check("منع بيع منتهي الصلاحية");
    private readonly CheckBox _changeSalesWarehouse = RefV2.Check("إمكانية تغيير مخزن البيع مع كل فاتورة");
    private readonly CheckBox _warningStrip = RefV2.Check("تشغيل شريط التحذيرات", true);
    private readonly NumericUpDown _expiredMonths = RefV2.Numeric(3, 0, 24);
    private readonly NumericUpDown _checksDays = RefV2.Numeric(2, 0, 365);
    private readonly ComboBox _defaultLanguage = RefV2.Combo(new[] { "الإنجليزية", "العربية" });
    private readonly ComboBox _programLanguage = RefV2.Combo(new[] { "الإنجليزية", "العربية" });
    private readonly ComboBox _searchLanguage = RefV2.Combo(new[] { "الإنجليزية", "العربية" });
    private readonly CheckBox _shortageWarning = RefV2.Check("التحذير لإضافة الصنف إلى شكل النواقص");
    private readonly CheckBox _shortageWithLimit = RefV2.Check("تشكيل النواقص مع حد الطلب", true);
    private readonly ComboBox _reportPrinter = RefV2.InstalledPrinters();
    private readonly CheckBox _cashDrawer = RefV2.Check("تشغيل درج النقدية");

    public OperatingSettingsV2Form() : base("إعدادات التشغيل", new Size(500, 670))
    {
        AddTitle("إعدادات التشغيل");
        BuildContent();
        LoadSettings();
        var save = RefV2.Button("حفظ", 90); save.Click += Save;
        var close = RefV2.Button("إغلاق", 90); close.Click += (_, _) => Close();
        Controls.Add(_scroll); Controls.Add(RefV2.Footer(close, save));
    }

    private void BuildContent()
    {
        var y = 8;
        AddPurchaseSection(ref y);
        AddSalesSection(ref y);
        AddAccountingSection(ref y);
        AddWarningSection(ref y);
        AddGeneralSection(ref y);
    }

    private void AddPurchaseSection(ref int y)
    {
        var box = RefV2.Section("المشتريات", RefV2.Lavender, 128); box.Location = new Point(10, y);
        Pair(box, "مخزن المشتريات الأساسي", _purchaseWarehouse, 95, 30, 230);
        _allowExpiredPurchase.Location = new Point(70, 63); _changePurchaseWarehouse.Location = new Point(70, 91);
        box.Controls.Add(_allowExpiredPurchase); box.Controls.Add(_changePurchaseWarehouse);
        _scroll.Controls.Add(box); y += 134;
    }

    private void AddSalesSection(ref int y)
    {
        var box = RefV2.Section("المبيعات", RefV2.Cyan, 258); box.Location = new Point(10, y);
        Pair(box, "مخزن المبيعات الأساسي", _salesWarehouse, 70, 30, 230);
        Pair(box, "عدد أيام فترة قبول مرتجع البيع", _returnDays, 230, 64, 74);
        _paidMessage.Location = new Point(62, 100); _expiryWarning.Location = new Point(62, 130); _preventExpired.Location = new Point(62, 160); _changeSalesWarehouse.Location = new Point(62, 190);
        box.Controls.AddRange(new Control[] { _paidMessage, _expiryWarning, _preventExpired, _changeSalesWarehouse });
        Pair(box, "الكاشير الأساسي", _cashier, 108, 220, 180);
        _scroll.Controls.Add(box); y += 264;
    }

    private void AddAccountingSection(ref int y)
    {
        var box = RefV2.Section("الحسابات", RefV2.Lavender, 160); box.Location = new Point(10, y);
        Pair(box, "موقع التوريدات النقدية", _cashSupply, 108, 32, 190);
        Pair(box, "موقع المصروفات النقدية", _cashExpense, 108, 72, 190);
        Pair(box, "حساب الشيكات الصادرة", _outgoingChecks, 108, 112, 190);
        _scroll.Controls.Add(box); y += 166;
    }

    private void AddWarningSection(ref int y)
    {
        var box = RefV2.Section("شريط التحذيرات", RefV2.Warning, 150); box.Location = new Point(10, y);
        _warningStrip.Location = new Point(70, 30); box.Controls.Add(_warningStrip);
        Pair(box, "إظهار الأدوية المنتهية قبل", _expiredMonths, 220, 62, 70);
        box.Controls.Add(new Label { Text = "شهر", Location = new Point(180, 65), AutoSize = true, Font = RefV2.Bold });
        Pair(box, "تحذير الشيكات المستحقة قبل", _checksDays, 220, 96, 70);
        box.Controls.Add(new Label { Text = "يوم", Location = new Point(180, 99), AutoSize = true, Font = RefV2.Bold });
        _scroll.Controls.Add(box); y += 156;
    }

    private void AddGeneralSection(ref int y)
    {
        var box = RefV2.Section("عام", RefV2.Lavender, 280); box.Location = new Point(10, y);
        Pair(box, "لغة البحث الافتراضية", _defaultLanguage, 145, 30, 150);
        Pair(box, "عرض الصنف في البرنامج باللغة", _programLanguage, 120, 66, 175);
        Pair(box, "عرض الصنف داخل شاشة البحث باللغة", _searchLanguage, 100, 102, 175);
        _shortageWarning.Location = new Point(65, 140); _shortageWithLimit.Location = new Point(65, 168);
        box.Controls.AddRange(new Control[] { _shortageWarning, _shortageWithLimit });
        Pair(box, "طابعة التقارير", _reportPrinter, 110, 202, 240);
        _cashDrawer.Location = new Point(65, 240); box.Controls.Add(_cashDrawer);
        _scroll.Controls.Add(box); y += 286;
    }

    private void LoadSettings()
    {
        _purchaseWarehouse.SelectedItem = RefV2.Setting(Settings, "ops.purchaseWarehouse", "الصيدلية");
        _salesWarehouse.SelectedItem = RefV2.Setting(Settings, "ops.salesWarehouse", "الصيدلية");
        _returnDays.Value = GetDecimal("ops.returnDays", 14);
        _cashier.SelectedItem = RefV2.Setting(Settings, "ops.cashier", "نقطة بيع 1");
        _cashSupply.SelectedItem = RefV2.Setting(Settings, "ops.cashSupply", "نقطة بيع 1");
        _cashExpense.SelectedItem = RefV2.Setting(Settings, "ops.cashExpense", "نقطة بيع 1");
        _outgoingChecks.SelectedItem = RefV2.Setting(Settings, "ops.outgoingChecks", "حساب بنك مصر");
        _allowExpiredPurchase.Checked = Settings.GetBool("ops.allowExpiredPurchase");
        _changePurchaseWarehouse.Checked = Settings.GetBool("ops.changePurchaseWarehouse");
        _paidMessage.Checked = Settings.GetBool("ops.paidMessage", true);
        _expiryWarning.Checked = Settings.GetBool("ops.expiryWarning", true);
        _preventExpired.Checked = Settings.GetBool("ops.preventExpired");
        _changeSalesWarehouse.Checked = Settings.GetBool("ops.changeSalesWarehouse");
        _warningStrip.Checked = Settings.GetBool("ops.warningStrip", true);
        _expiredMonths.Value = GetDecimal("ops.expiredMonths", 3);
        _checksDays.Value = GetDecimal("ops.checksDays", 2);
        _shortageWarning.Checked = Settings.GetBool("ops.shortageWarning");
        _shortageWithLimit.Checked = Settings.GetBool("ops.shortageWithLimit", true);
        var language = RefV2.Setting(Settings, "ops.searchLanguage", "الإنجليزية"); _defaultLanguage.SelectedItem = language; _programLanguage.SelectedItem = language; _searchLanguage.SelectedItem = language;
        var printer = RefV2.Setting(Settings, "ops.reportPrinter"); if (!string.IsNullOrWhiteSpace(printer)) _reportPrinter.SelectedItem = printer;
        _cashDrawer.Checked = Settings.GetBool("ops.cashDrawer");
    }

    private decimal GetDecimal(string key, decimal fallback) => decimal.TryParse(Settings.Get(key), out var v) ? v : fallback;

    private void Save(object? sender, EventArgs e)
    {
        Settings.Set("ops.purchaseWarehouse", _purchaseWarehouse.Text);
        Settings.Set("ops.salesWarehouse", _salesWarehouse.Text);
        Settings.Set("ops.returnDays", _returnDays.Value.ToString());
        Settings.Set("ops.cashier", _cashier.Text);
        Settings.Set("ops.cashSupply", _cashSupply.Text);
        Settings.Set("ops.cashExpense", _cashExpense.Text);
        Settings.Set("ops.outgoingChecks", _outgoingChecks.Text);
        Settings.SetBool("ops.allowExpiredPurchase", _allowExpiredPurchase.Checked);
        Settings.SetBool("ops.changePurchaseWarehouse", _changePurchaseWarehouse.Checked);
        Settings.SetBool("ops.paidMessage", _paidMessage.Checked);
        Settings.SetBool("ops.expiryWarning", _expiryWarning.Checked);
        Settings.SetBool("ops.preventExpired", _preventExpired.Checked);
        Settings.SetBool("ops.changeSalesWarehouse", _changeSalesWarehouse.Checked);
        Settings.SetBool("ops.warningStrip", _warningStrip.Checked);
        Settings.Set("ops.expiredMonths", _expiredMonths.Value.ToString());
        Settings.Set("ops.checksDays", _checksDays.Value.ToString());
        Settings.Set("ops.searchLanguage", _defaultLanguage.Text);
        Settings.SetBool("ops.shortageWarning", _shortageWarning.Checked);
        Settings.SetBool("ops.shortageWithLimit", _shortageWithLimit.Checked);
        Settings.Set("ops.reportPrinter", _reportPrinter.Text);
        Settings.SetBool("ops.cashDrawer", _cashDrawer.Checked);
        MessageBox.Show(this, "تم حفظ إعدادات التشغيل.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public sealed class BarcodePrintSettingsV2Form : ReferenceV2Base
{
    private readonly ComboBox _printer;
    private readonly CheckBox _none;
    private readonly CheckBox _thermal;
    private readonly CheckBox _a4;
    private readonly CheckBox _printOrg;
    private readonly CheckBox _printPhone;
    private readonly TextBox _org;
    private readonly TextBox _phone;
    private readonly ComboBox _paper;
    private readonly Label _preview;

    public BarcodePrintSettingsV2Form() : base("إعدادات طباعة الباركود", new Size(580, 590))
    {
        AddTitle("إعدادات طباعة الباركود");
        _printer = RefV2.InstalledPrinters();
        _none = RefV2.Check("لا يوجد طباعة باركود");
        _thermal = RefV2.Check("طباعة على طابعة حرارية", true);
        _a4 = RefV2.Check("طباعة على طابعة A4");
        _printOrg = RefV2.Check("طباعة اسم المؤسسة", true);
        _printPhone = RefV2.Check("طباعة رقم التليفون", true);
        _org = RefV2.TextBox(RefV2.Setting(Settings, "barcode.org", "صيدلية ياسين"));
        _phone = RefV2.TextBox(RefV2.Setting(Settings, "barcode.phone", "0502338665"));
        _paper = RefV2.Combo(new[] { "6 x 24", "5 x 13", "A4" }, RefV2.Setting(Settings, "barcode.paper", "6 x 24"));
        _preview = new Label { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Tahoma", 10F, FontStyle.Bold) };

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.FromArgb(240, 240, 240) };
        var flags = new Panel { Location = new Point(14, 8), Size = new Size(540, 44), BackColor = RefV2.Green, BorderStyle = BorderStyle.FixedSingle };
        _none.SetBounds(330, 8, 195, 26); _thermal.SetBounds(155, 8, 170, 26); _a4.SetBounds(8, 8, 140, 26); flags.Controls.AddRange(new Control[] { _none, _thermal, _a4 });
        Pair(body, "اسم الطابعة", _printer, 25, 68, 350);
        Pair(body, "اسم المؤسسة", _org, 130, 110, 220);
        Pair(body, "رقم التليفون", _phone, 130, 150, 220);
        _printOrg.SetBounds(385, 110, 150, 26); _printPhone.SetBounds(385, 150, 150, 26); body.Controls.Add(_printOrg); body.Controls.Add(_printPhone);
        _preview.SetBounds(145, 190, 300, 90); body.Controls.Add(_preview);
        var paperBox = new Panel { Location = new Point(145, 315), Size = new Size(300, 70), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 245, 245) };
        paperBox.Controls.Add(new Label { Text = "في حالة الطباعة على ورق A4", Dock = DockStyle.Top, Height = 28, Font = RefV2.Bold, TextAlign = ContentAlignment.MiddleCenter });
        _paper.SetBounds(50, 34, 170, 25); paperBox.Controls.Add(_paper);
        var close = RefV2.Button("إغلاق"); close.SetBounds(175, 410, 90, 30); close.Click += (_, _) => Close();
        var save = RefV2.Button("حفظ"); save.SetBounds(315, 410, 90, 30); save.Click += Save;
        body.Controls.AddRange(new Control[] { flags, paperBox, close, save }); Controls.Add(body);
        foreach (var control in new Control[] { _printer, _none, _thermal, _a4, _printOrg, _printPhone, _org, _phone, _paper }) control.Click += (_, _) => UpdatePreview();
        foreach (var control in new Control[] { _org, _phone }) control.TextChanged += (_, _) => UpdatePreview();
        _printer.SelectedIndexChanged += (_, _) => UpdatePreview();
        _printOrg.CheckedChanged += (_, _) => UpdatePreview(); _printPhone.CheckedChanged += (_, _) => UpdatePreview();
        _thermal.CheckedChanged += (_, _) => UpdatePreview(); _a4.CheckedChanged += (_, _) => UpdatePreview(); _none.CheckedChanged += (_, _) => UpdatePreview();
        LoadSettings();
        UpdatePreview();
    }

    private void LoadSettings()
    {
        var savedPrinter = RefV2.Setting(Settings, "barcode.printer"); if (!string.IsNullOrWhiteSpace(savedPrinter)) _printer.SelectedItem = savedPrinter;
        _printOrg.Checked = Settings.GetBool("barcode.printOrg", true); _printPhone.Checked = Settings.GetBool("barcode.printPhone", true);
        _none.Checked = Settings.GetBool("barcode.none"); _thermal.Checked = Settings.GetBool("barcode.thermal", true); _a4.Checked = Settings.GetBool("barcode.a4");
    }

    private void UpdatePreview()
    {
        if (_none.Checked)
        {
            _preview.Text = "لا يوجد طباعة باركود"; return;
        }
        var org = _printOrg.Checked ? _org.Text : "";
        var phone = _printPhone.Checked ? _phone.Text : "";
        var mode = _a4.Checked ? "A4" : (_thermal.Checked ? "حراري" : "");
        _preview.Text = $"{phone}    {org}\n\n||||||||||||||||||||||||||||||\nاسم الصنف مكتوب هنا     4.00 E.L.\n{mode}    {_printer.Text}";
    }

    private void Save(object? sender, EventArgs e)
    {
        Settings.Set("barcode.printer", _printer.Text); Settings.Set("barcode.org", _org.Text); Settings.Set("barcode.phone", _phone.Text); Settings.Set("barcode.paper", _paper.Text);
        Settings.SetBool("barcode.printOrg", _printOrg.Checked); Settings.SetBool("barcode.printPhone", _printPhone.Checked); Settings.SetBool("barcode.none", _none.Checked); Settings.SetBool("barcode.thermal", _thermal.Checked); Settings.SetBool("barcode.a4", _a4.Checked);
        MessageBox.Show(this, "تم حفظ إعدادات طباعة الباركود.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public sealed class SalesInvoicePrintSettingsV2Form : ReferenceV2Base
{
    private readonly ComboBox _printer;
    private readonly ComboBox _type = RefV2.Combo(new[] { "ريستوت", "A4", "حراري" });
    private readonly TextBox _intro = RefV2.TextBox("تمنا لكم الشفاء العاجل");
    private readonly TextBox _org = RefV2.TextBox("صيدلية ياسين");
    private readonly TextBox _phone = RefV2.TextBox("0502338665 - 01007535159");
    private readonly TextBox _ending = RefV2.TextBox("أصناف الفاتورة لا ترجع");
    private readonly TextBox _tax = RefV2.TextBox("503502324");
    private readonly TextBox _commercial = RefV2.TextBox("44669");
    private readonly NumericUpDown _template = RefV2.Numeric(5, 0, 99);
    private readonly NumericUpDown _minimum = RefV2.Numeric(10, 0, 99999);
    private readonly NumericUpDown _copies = RefV2.Numeric(1, 0, 99);
    private readonly CheckBox _entered = RefV2.Check("طباعة الفاتورة كما تم إدخالها");
    private readonly CheckBox _customerName = RefV2.Check("طباعة اسم العميل", true);
    private readonly CheckBox _sellerName = RefV2.Check("طباعة اسم البائع");
    private readonly Label _preview;

    public SalesInvoicePrintSettingsV2Form() : base("إعدادات طباعة فاتورة البيع", new Size(1000, 450))
    {
        AddTitle("إعدادات طباعة فاتورة البيع");
        _printer = RefV2.InstalledPrinters();
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        _preview = new Label { Location = new Point(12, 76), Size = new Size(275, 315), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 8.5F, FontStyle.Bold), TextAlign = ContentAlignment.TopCenter };
        body.Controls.Add(_preview);
        Pair(body, "نوع الطباعة", _type, 310, 76, 300); Pair(body, "مقدمة الفاتورة", _intro, 310, 116, 300); Pair(body, "اسم المؤسسة", _org, 310, 156, 300); Pair(body, "رقم التليفون", _phone, 310, 196, 300); Pair(body, "نهاية الفاتورة", _ending, 310, 236, 300); Pair(body, "السجل الضريبي", _tax, 310, 276, 300); Pair(body, "السجل التجاري", _commercial, 310, 316, 300);
        Pair(body, "نموذج", _template, 690, 76, 70); Pair(body, "أقل قيمة للفاتورة لطباعتها", _minimum, 690, 116, 70); Pair(body, "عدد النسخ المطبوعة", _copies, 690, 156, 70);
        Pair(body, "اسم الطابعة", _printer, 690, 196, 250); _customerName.Location = new Point(690, 242); _sellerName.Location = new Point(690, 270); _entered.Location = new Point(690, 298); body.Controls.AddRange(new Control[] { _customerName, _sellerName, _entered });
        var close = RefV2.Button("إغلاق"); close.SetBounds(770, 350, 90, 30); close.Click += (_, _) => Close(); var save = RefV2.Button("حفظ"); save.SetBounds(650, 350, 90, 30); save.Click += Save;
        body.Controls.AddRange(new Control[] { close, save }); Controls.Add(body);
        foreach (var c in new Control[] { _type, _intro, _org, _phone, _ending, _tax, _commercial, _template, _minimum, _copies, _printer, _customerName, _sellerName, _entered })
        { if (c is TextBox t) t.TextChanged += (_, _) => UpdatePreview(); else if (c is NumericUpDown n) n.ValueChanged += (_, _) => UpdatePreview(); else if (c is ComboBox cb) cb.SelectedIndexChanged += (_, _) => UpdatePreview(); else if (c is CheckBox ch) ch.CheckedChanged += (_, _) => UpdatePreview(); }
        LoadSettings(); UpdatePreview();
    }

    private void LoadSettings()
    {
        var p = RefV2.Setting(Settings, "invoice.printer"); if (!string.IsNullOrWhiteSpace(p)) _printer.SelectedItem = p;
        var v = RefV2.Setting(Settings, "invoice.type", "ريستوت"); _type.SelectedItem = v;
        _intro.Text = RefV2.Setting(Settings, "invoice.intro", _intro.Text); _org.Text = RefV2.Setting(Settings, "invoice.org", _org.Text); _phone.Text = RefV2.Setting(Settings, "invoice.phone", _phone.Text); _ending.Text = RefV2.Setting(Settings, "invoice.ending", _ending.Text); _tax.Text = RefV2.Setting(Settings, "invoice.tax", _tax.Text); _commercial.Text = RefV2.Setting(Settings, "invoice.commercial", _commercial.Text);
        _template.Value = GetDecimal("invoice.template", 5, 99); _minimum.Value = GetDecimal("invoice.minimum", 10, 99999); _copies.Value = GetDecimal("invoice.copies", 1, 99);
        _customerName.Checked = Settings.GetBool("invoice.customerName", true); _sellerName.Checked = Settings.GetBool("invoice.sellerName"); _entered.Checked = Settings.GetBool("invoice.entered");
    }

    private decimal GetDecimal(string key, decimal fallback, decimal max) => decimal.TryParse(Settings.Get(key), out var v) ? Math.Min(Math.Max(v, 0), max) : fallback;

    private void UpdatePreview()
    {
        _preview.Text = $"{_org.Text}\n{_phone.Text}\n\n{_intro.Text}\n\nاسم الصنف    الكمية    الوحدة    الإجمالي\n────────────────────\nبروفين 200    1       شريط      21.00\nترانزيفرين    1       شريط      13.00\n\nالإجمالي: 51.00\nالمطلوب: 51.00\n\n{_ending.Text}\n\n{_type.Text}  |  نسخ: {_copies.Value}";
    }

    private void Save(object? sender, EventArgs e)
    {
        Settings.Set("invoice.printer", _printer.Text); Settings.Set("invoice.type", _type.Text); Settings.Set("invoice.intro", _intro.Text); Settings.Set("invoice.org", _org.Text); Settings.Set("invoice.phone", _phone.Text); Settings.Set("invoice.ending", _ending.Text); Settings.Set("invoice.tax", _tax.Text); Settings.Set("invoice.commercial", _commercial.Text);
        Settings.Set("invoice.template", _template.Value.ToString()); Settings.Set("invoice.minimum", _minimum.Value.ToString()); Settings.Set("invoice.copies", _copies.Value.ToString()); Settings.SetBool("invoice.customerName", _customerName.Checked); Settings.SetBool("invoice.sellerName", _sellerName.Checked); Settings.SetBool("invoice.entered", _entered.Checked);
        MessageBox.Show(this, "تم حفظ إعدادات طباعة فاتورة البيع.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

public sealed class BackupV2Form : ReferenceV2Base
{
    private readonly TextBox _path = RefV2.TextBox();
    public BackupV2Form() : base("أخذ نسخة احتياطية", new Size(580, 170))
    {
        AddTitle("أخذ نسخة احتياطية");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        Pair(body, "مسار حفظ النسخة", _path, 80, 20, 360);
        var browse = RefV2.Button("استعراض", 85); browse.SetBounds(5, 18, 70, 30); browse.Click += Browse;
        var ok = RefV2.Button("موافق"); ok.SetBounds(300, 65, 95, 30); ok.Click += Backup;
        var close = RefV2.Button("إغلاق"); close.SetBounds(190, 65, 95, 30); close.Click += (_, _) => Close();
        body.Controls.AddRange(new Control[] { browse, ok, close }); Controls.Add(body);
        _path.Text = RefV2.Setting(Settings, "backup.path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Glowva Backups"));
    }
    private void Browse(object? s, EventArgs e) { using var d = new FolderBrowserDialog(); if (d.ShowDialog(this) == DialogResult.OK) _path.Text = d.SelectedPath; }
    private void Backup(object? s, EventArgs e)
    {
        try { var path = BackupService.CreateBackup(_path.Text); Settings.Set("backup.path", _path.Text); MessageBox.Show(this, $"تم إنشاء النسخة الاحتياطية:\n{path}", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

public sealed class ScheduledBackupV2Form : ReferenceV2Base
{
    private sealed record Entry(string Name, string Directory, int EveryHours, int DeleteAfterDays, string From, string To, string Start, string Stop, bool Enabled);
    private readonly ListBox _operations = new() { IntegralHeight = false };
    private readonly TextBox _name = RefV2.TextBox();
    private readonly TextBox _path = RefV2.TextBox();
    private readonly NumericUpDown _every = RefV2.Numeric(3, 1, 168);
    private readonly NumericUpDown _delete = RefV2.Numeric(3, 0, 3650);
    private readonly ComboBox _from = RefV2.Combo(Enumerable.Range(0, 24).Select(h => $"{h:00}:00:00"));
    private readonly ComboBox _to = RefV2.Combo(Enumerable.Range(0, 24).Select(h => $"{h:00}:00:00"));
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd", Width = 145 };
    private readonly DateTimePicker _stop = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd", Width = 145, ShowCheckBox = true };
    private readonly CheckBox _enabled = RefV2.Check("تشغيل العملية", true);
    private List<Entry> _items = new();

    public ScheduledBackupV2Form() : base("النسخ الاحتياطية الدورية", new Size(620, 500))
    {
        AddTitle("النسخ الاحتياطية الدورية");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var listPanel = new Panel { Location = new Point(10, 8), Size = new Size(560, 125), BackColor = Color.FromArgb(245, 245, 245), BorderStyle = BorderStyle.FixedSingle };
        _operations.SetBounds(8, 8, 544, 105); listPanel.Controls.Add(_operations); body.Controls.Add(listPanel);
        Pair(body, "اسم العملية", _name, 100, 150, 190); Pair(body, "مسار حفظ النسخ", _path, 100, 188, 330);
        Pair(body, "أخذ نسخة كل", _every, 300, 226, 70); Pair(body, "حذف النسخ التي من قبل", _delete, 90, 226, 70);
        Pair(body, "من الساعة", _from, 100, 264, 135); Pair(body, "إلى الساعة", _to, 340, 264, 135);
        Pair(body, "ابتداء من يوم", _start, 100, 302, 145); Pair(body, "تاريخ الإيقاف", _stop, 340, 302, 145);
        _enabled.SetBounds(100, 340, 170, 24); body.Controls.Add(_enabled);
        var browse = RefV2.Button("استعراض", 80); browse.SetBounds(440, 188, 80, 30); browse.Click += (_, _) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog(this) == DialogResult.OK) _path.Text = d.SelectedPath; };
        var newB = RefV2.Button("عملية جديدة"); newB.SetBounds(80, 385, 100, 30); newB.Click += (_, _) => ClearEditor();
        var save = RefV2.Button("حفظ"); save.SetBounds(190, 385, 90, 30); save.Click += SaveEntry;
        var run = RefV2.Button("تشغيل العملية"); run.SetBounds(290, 385, 105, 30); run.Click += RunEntry;
        var delete = RefV2.Button("حذف العملية"); delete.SetBounds(405, 385, 105, 30); delete.Click += DeleteEntry;
        var close = RefV2.Button("إغلاق"); close.SetBounds(510, 385, 70, 30); close.Click += (_, _) => Close();
        body.Controls.AddRange(new Control[] { browse, newB, save, run, delete, close }); Controls.Add(body);
        _operations.SelectedIndexChanged += (_, _) => LoadSelected();
        LoadData();
    }

    private void LoadData()
    {
        try { _items = JsonSerializer.Deserialize<List<Entry>>(Settings.Get("backup.scheduled.json") ?? "[]") ?? new(); } catch { _items = new(); }
        RefreshList(); ClearEditor();
    }

    private void RefreshList() { _operations.Items.Clear(); foreach (var item in _items) _operations.Items.Add($"{item.Name} | كل {item.EveryHours} ساعة"); }

    private void ClearEditor()
    {
        _name.Clear(); _path.Text = RefV2.Setting(Settings, "backup.path", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Glowva Backups")); _every.Value = 3; _delete.Value = 3; _from.SelectedIndex = 1; _to.SelectedIndex = 23; _start.Value = DateTime.Today; _stop.Checked = false; _enabled.Checked = true; _operations.ClearSelected();
    }

    private void LoadSelected()
    {
        var i = _operations.SelectedIndex; if (i < 0 || i >= _items.Count) return; var x = _items[i];
        _name.Text = x.Name; _path.Text = x.Directory; _every.Value = x.EveryHours; _delete.Value = x.DeleteAfterDays; _from.SelectedItem = x.From; _to.SelectedItem = x.To; if (DateTime.TryParse(x.Start, out var st)) _start.Value = st; if (DateTime.TryParse(x.Stop, out var sp)) { _stop.Checked = true; _stop.Value = sp; } else _stop.Checked = false; _enabled.Checked = x.Enabled;
    }

    private void SaveEntry(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show(this, "أدخل اسم العملية."); return; }
        var entry = new Entry(_name.Text.Trim(), _path.Text.Trim(), (int)_every.Value, (int)_delete.Value, _from.Text, _to.Text, _start.Value.ToString("yyyy-MM-dd"), _stop.Checked ? _stop.Value.ToString("yyyy-MM-dd") : "", _enabled.Checked);
        var i = _operations.SelectedIndex; if (i >= 0 && i < _items.Count) _items[i] = entry; else _items.Add(entry);
        SaveData(); RefreshList(); MessageBox.Show(this, "تم حفظ العملية.");
    }

    private void RunEntry(object? s, EventArgs e)
    {
        try { var directory = string.IsNullOrWhiteSpace(_path.Text) ? null : _path.Text; var path = BackupService.CreateBackup(directory); MessageBox.Show(this, $"تم تشغيل العملية وإنشاء النسخة:\n{path}"); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void DeleteEntry(object? s, EventArgs e)
    {
        var i = _operations.SelectedIndex; if (i < 0) return; _items.RemoveAt(i); SaveData(); RefreshList(); ClearEditor();
    }

    private void SaveData() { Settings.Set("backup.scheduled.json", JsonSerializer.Serialize(_items)); Settings.Set("backup.path", _path.Text); }
}

public sealed class DatabaseSizeV2Form : ReferenceV2Base
{
    private readonly Label _size = new() { Dock = DockStyle.Fill, Font = new Font("Tahoma", 18F, FontStyle.Bold), ForeColor = Color.Red, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(248, 248, 248) };
    public DatabaseSizeV2Form() : base("حجم قاعدة البيانات", new Size(420, 185))
    {
        AddTitle("حجم قاعدة البيانات");
        Controls.Add(_size);
        Controls.Add(new Label { Text = "حجم قاعدة البيانات", Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleRight, Font = RefV2.Bold, Padding = new Padding(0, 0, 10, 0) });
        Shown += (_, _) => RefreshSize();
    }
    private void RefreshSize() { try { var mb = File.Exists(Data.Database.Database.DatabasePath) ? new FileInfo(Data.Database.Database.DatabasePath).Length / 1024d / 1024d : 0; _size.Text = $"MB {mb:0.00}"; } catch { _size.Text = "MB 0.00"; } }
}

public sealed class BarcodePrintV2Form : ReferenceV2Base
{
    private readonly DataGridView _grid = new() { AllowUserToAddRows = false, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, RightToLeft = RightToLeft.Yes };
    private readonly Label _count = new() { Text = "0", ForeColor = Color.Red, Font = new Font("Tahoma", 12F, FontStyle.Bold), AutoSize = true };
    public BarcodePrintV2Form() : base("طباعة باركود", new Size(1100, 760), true)
    {
        AddTitle("طباعة باركود");
        _grid.SetBounds(15, 76, 1070, 575); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        AddColumn("كود الصنف", 160); AddColumn("اسم الصنف", 320); AddColumn("الوحدة", 140); AddColumn("الكمية", 110); AddColumn("تاريخ الصلاحية", 170); AddColumn("سعر البيع", 120);
        _grid.RowPrePaint += (_, e) => { if (e.RowIndex >= 0) _grid.Rows[e.RowIndex].DefaultCellStyle.Font = RefV2.Normal; };
        Controls.Add(_grid);
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(245, 245, 245) };
        var close = RefV2.Button("إغلاق"); close.SetBounds(22, 18, 90, 30); close.Click += (_, _) => Close();
        var add = RefV2.Button("إضافة صنف"); add.SetBounds(925, 18, 95, 30); add.Click += AddProduct;
        var del = RefV2.Button("حذف صنف"); del.SetBounds(820, 18, 95, 30); del.Click += (_, _) => { if (_grid.CurrentRow != null) _grid.Rows.Remove(_grid.CurrentRow); UpdateCount(); };
        var print = RefV2.Button("طباعة باركود", 110); print.SetBounds(690, 18, 110, 30); print.Click += Print;
        var label = RefV2.L("عدد الباركود", 110); label.SetBounds(455, 20, 110, 28); _count.SetBounds(575, 19, 40, 28);
        bottom.Controls.AddRange(new Control[] { close, add, del, print, label, _count }); Controls.Add(bottom);
    }
    private void AddColumn(string header, int width) => _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, Width = width });
    private void AddProduct(object? s, EventArgs e)
    {
        using var dialog = new Form { Text = "إضافة صنف", ClientSize = new Size(400, 170), StartPosition = FormStartPosition.CenterParent, RightToLeft = RightToLeft.Yes, RightToLeftLayout = true };
        var code = RefV2.TextBox(); code.SetBounds(90, 25, 220, 25); var qty = RefV2.Numeric(1, 1, 9999); qty.SetBounds(90, 60, 70, 25); Pair(dialog, "كود الصنف", code, 10, 25, 220); Pair(dialog, "الكمية", qty, 10, 60, 70); var ok = RefV2.Button("موافق"); ok.SetBounds(220, 105, 90, 30); ok.DialogResult = DialogResult.OK; dialog.Controls.Add(ok); if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _grid.Rows.Add(code.Text, "", "", qty.Value.ToString("0.##"), "", ""); UpdateCount();
    }
    private void Print(object? s, EventArgs e) { MessageBox.Show(this, $"تم تجهيز {_count.Text} باركود للطباعة. استخدم إعدادات الطباعة المحددة.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private void UpdateCount() { decimal total = 0; foreach (DataGridViewRow row in _grid.Rows) if (decimal.TryParse(Convert.ToString(row.Cells[3].Value), out var q)) total += q; _count.Text = total.ToString("0.##"); }
}

public sealed class ContractInvoiceV2Form : ReferenceV2Base
{
    private readonly ComboBox _printer;
    public ContractInvoiceV2Form() : base("إصدار فاتورة ورقية للتعاقد", new Size(880, 650))
    {
        AddTitle("إصدار فاتورة ورقية للتعاقد");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        var left = new Panel { Location = new Point(10, 65), Size = new Size(210, 515), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        left.Controls.Add(new Label { Text = "الصحة خير من الرزق\nصيدلية ابن سينا\n22334455/س\n\nاسم الصنف    الكمية    الوحدة    الإجمالي\n────────────────────────\nبروفين 200    1      شريط     21.00\nترانزيفرين    1      شريط     13.00\n\nعدد القطع              2.25\nالإجمالي               51.00\nالمطلوب                51.00\n\nتاريخ الفاتورة\n15/11/2015 10:34:41\n||||||||||||||||||||||\nأصناف الفاتورة لا ترجع", Dock = DockStyle.Fill, Font = new Font("Tahoma", 8.2F, FontStyle.Bold), TextAlign = ContentAlignment.TopCenter });
        body.Controls.Add(left);
        _printer = RefV2.InstalledPrinters(RefV2.Setting(Settings, "invoice.printer"));
        var right = new Panel { Location = new Point(235, 65), Size = new Size(625, 515) };
        Pair(right, "نوع الطباعة", RefV2.Combo(new[] { "ريستوت", "A4", "حراري" }), 70, 15, 390);
        Pair(right, "مقدمة الفاتورة", RefV2.TextBox(RefV2.Setting(Settings, "invoice.intro", "تمنا لكم الشفاء العاجل")), 70, 55, 390);
        Pair(right, "اسم المؤسسة", RefV2.TextBox(RefV2.Setting(Settings, "invoice.org", "صيدلية ياسين")), 70, 95, 390);
        Pair(right, "رقم التليفون", RefV2.TextBox(RefV2.Setting(Settings, "invoice.phone", "0502338665 - 01007535159")), 70, 135, 390);
        Pair(right, "نهاية الفاتورة", RefV2.TextBox(RefV2.Setting(Settings, "invoice.ending", "أصناف الفاتورة لا ترجع")), 70, 175, 390);
        Pair(right, "السجل الضريبي", RefV2.TextBox(RefV2.Setting(Settings, "invoice.tax", "503502324")), 70, 215, 390);
        Pair(right, "السجل التجاري", RefV2.TextBox(RefV2.Setting(Settings, "invoice.commercial", "44669")), 70, 255, 390);
        Pair(right, "اسم الطابعة", _printer, 70, 395, 390);
        var close = RefV2.Button("إغلاق"); close.SetBounds(430, 455, 90, 30); close.Click += (_, _) => Close();
        var save = RefV2.Button("حفظ"); save.SetBounds(290, 455, 90, 30); save.Click += Save;
        right.Controls.AddRange(new Control[] { close, save }); body.Controls.Add(right); Controls.Add(body);
    }
    private void Save(object? s, EventArgs e) { Settings.Set("invoice.printer", _printer.Text); MessageBox.Show(this, "تم حفظ إعدادات الفاتورة.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information); }
}
