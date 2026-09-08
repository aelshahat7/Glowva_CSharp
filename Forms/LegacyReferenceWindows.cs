using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GlowvaERP.Data;
using GlowvaERP.Services;

namespace GlowvaERP.Forms;

internal static class ReferenceWindowStyle
{
    public static readonly Color Gold = Color.FromArgb(255, 204, 74);
    public static readonly Color TitleBlue = Color.FromArgb(0, 90, 205);
    public static readonly Color PaleGray = Color.FromArgb(245, 245, 245);
    public static readonly Color SoftCream = Color.FromArgb(255, 248, 230);
    public static readonly Color SoftYellow = Color.FromArgb(255, 252, 210);
    public static readonly Color SoftGreen = Color.FromArgb(205, 255, 210);
    public static readonly Color SoftCyan = Color.FromArgb(220, 250, 252);
    public static readonly Color SoftLavender = Color.FromArgb(238, 232, 255);
    public static readonly Color Orange = Color.FromArgb(244, 210, 176);
    public static readonly Color Red = Color.FromArgb(220, 0, 0);
    public static readonly Font Normal = new("Tahoma", 9.5F, FontStyle.Regular);
    public static readonly Font Bold = new("Tahoma", 10F, FontStyle.Bold);
    public static readonly Font Title = new("Tahoma", 22F, FontStyle.Bold);

    public static void Prepare(Form form, Size size, bool dialog = true)
    {
        form.ClientSize = size;
        form.StartPosition = FormStartPosition.CenterScreen;
        form.RightToLeft = RightToLeft.Yes;
        form.RightToLeftLayout = true;
        form.Font = Normal;
        form.BackColor = Color.FromArgb(240, 240, 240);
        form.FormBorderStyle = FormBorderStyle.FixedSingle;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ShowInTaskbar = !dialog;
    }

    public static Label TitleLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 66,
            Font = Title,
            ForeColor = TitleBlue,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(245, 245, 245),
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    public static Button Button(string text, int width = 95)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            Font = Bold,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = true,
            Margin = new Padding(5),
            RightToLeft = RightToLeft.Yes
        };
    }

    public static TextBox TextBox(string text = "")
    {
        return new TextBox
        {
            Text = text,
            Height = 28,
            Font = Normal,
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            RightToLeft = RightToLeft.Yes
        };
    }

    public static ComboBox Combo(params string[] items)
    {
        var c = new ComboBox
        {
            Height = 28,
            Font = Normal,
            DropDownStyle = ComboBoxStyle.DropDownList,
            RightToLeft = RightToLeft.Yes
        };
        c.Items.AddRange(items);
        if (c.Items.Count > 0) c.SelectedIndex = 0;
        return c;
    }

    public static CheckBox Check(string text, bool value = false)
    {
        return new CheckBox
        {
            Text = text,
            Checked = value,
            AutoSize = true,
            Font = Bold,
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    public static NumericUpDown Numeric(int value = 1, int min = 0, int max = 9999)
    {
        return new NumericUpDown
        {
            Value = value,
            Minimum = min,
            Maximum = max,
            Width = 74,
            Height = 28,
            Font = Bold,
            TextAlign = HorizontalAlignment.Right
        };
    }
}

public abstract class ReferenceWindowBase : Form
{
    protected ReferenceWindowBase(string text, Size size)
    {
        Text = text;
        ReferenceWindowStyle.Prepare(this, size);
    }

    protected void AddTitle(string title) => Controls.Add(ReferenceWindowStyle.TitleLabel(title));

    protected static FlowLayoutPanel Footer(params Button[] buttons)
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(8, 5, 8, 5),
            BackColor = Color.FromArgb(238, 238, 238),
            RightToLeft = RightToLeft.Yes
        };
        foreach (var button in buttons) footer.Controls.Add(button);
        return footer;
    }
}

public sealed class OrganizationDataForm : ReferenceWindowBase
{
    public OrganizationDataForm() : base("بيانات المؤسسة", new Size(930, 620))
    {
        AddTitle("بيانات المؤسسة");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), AutoScroll = true, BackColor = Color.FromArgb(240, 240, 240) };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, Height = 300, ColumnCount = 4, RowCount = 5, RightToLeft = RightToLeft.Yes };
        for (var i = 0; i < 4; i++) fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var i = 0; i < 5; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        AddField(fields, "الكود", "1", 0, 0, 1);
        AddField(fields, "الاسم العربي", "الصيدلية", 1, 0, 1);
        AddField(fields, "الاسم الإنجليزي", "", 2, 0, 2);
        AddField(fields, "رقم السجل التجاري", "", 0, 1, 2);
        AddField(fields, "رقم البطاقة الضريبية", "", 2, 1, 1);
        AddField(fields, "صاحب المؤسسة", "", 0, 2, 2);
        AddField(fields, "المدير المسئول", "", 2, 2, 2);
        AddField(fields, "التليفون", "", 0, 3, 1);
        AddField(fields, "الفاكس", "", 1, 3, 1);
        AddField(fields, "العنوان", "", 2, 3, 2);

        var image = new Panel { Width = 170, Height = 120, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Location = new Point(30, 325) };
        var choose = ReferenceWindowStyle.Button("اختيار الصورة", 115);
        choose.Location = new Point(58, 447);
        body.Controls.Add(fields);
        body.Controls.Add(image);
        body.Controls.Add(choose);

        var save = ReferenceWindowStyle.Button("حفظ", 95);
        save.Click += (_, _) => MessageBox.Show(this, "تم الحفظ.");
        var close = ReferenceWindowStyle.Button("إغلاق", 95);
        close.Click += (_, _) => Close();
        Controls.Add(body);
        Controls.Add(Footer(close, save));
    }

    private static void AddField(TableLayoutPanel p, string label, string value, int col, int row, int span)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        var l = new Label { Text = label, Dock = DockStyle.Right, Width = 150, TextAlign = ContentAlignment.MiddleRight, Font = ReferenceWindowStyle.Bold };
        var t = ReferenceWindowStyle.TextBox(value); t.Dock = DockStyle.Fill;
        panel.Controls.Add(t); panel.Controls.Add(l);
        p.Controls.Add(panel, col, row);
        if (span > 1) p.SetColumnSpan(panel, span);
    }
}

public sealed class OperatingSettingsForm : ReferenceWindowBase
{
    private readonly FlowLayoutPanel _body = new();

    public OperatingSettingsForm() : base("إعدادات التشغيل", new Size(500, 670))
    {
        AddTitle("إعدادات التشغيل");
        _body.Dock = DockStyle.Fill;
        _body.FlowDirection = FlowDirection.TopDown;
        _body.WrapContents = false;
        _body.AutoScroll = true;
        _body.Padding = new Padding(10, 8, 10, 0);
        _body.RightToLeft = RightToLeft.Yes;
        _body.BackColor = Color.FromArgb(245, 245, 245);

        AddPurchasesSection();
        AddSalesSection();
        AddAccountingSection();
        AddGeneralSection();

        var save = ReferenceWindowStyle.Button("حفظ", 90); save.Click += (_, _) => MessageBox.Show(this, "تم حفظ الإعدادات.");
        var close = ReferenceWindowStyle.Button("إغلاق", 90); close.Click += (_, _) => Close();
        Controls.Add(_body); Controls.Add(Footer(close, save));
    }

    private void AddPurchasesSection()
    {
        var box = Section("المشتريات", ReferenceWindowStyle.SoftLavender, 120);
        var stock = ReferenceWindowStyle.Combo("الصيدلية"); stock.SetBounds(68, 36, 250, 28);
        box.Controls.Add(stock);
        box.Controls.Add(new Label { Text = "مخزن المشتريات الأساسي", Location = new Point(322, 40), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var c1 = ReferenceWindowStyle.Check("السماح بتسجيل أصناف منتهية الصلاحية في المشتريات"); c1.SetBounds(68, 69, 370, 26);
        var c2 = ReferenceWindowStyle.Check("إمكانية تغيير مخزن المشتريات مع كل فاتورة"); c2.SetBounds(68, 96, 370, 26);
        box.Controls.Add(c1); box.Controls.Add(c2); _body.Controls.Add(box);
    }

    private void AddSalesSection()
    {
        var box = Section("المبيعات", ReferenceWindowStyle.SoftCyan, 270);
        var combo = ReferenceWindowStyle.Combo("الصيدلية"); combo.SetBounds(68, 36, 250, 28); box.Controls.Add(combo);
        box.Controls.Add(new Label { Text = "مخزن المبيعات الأساسي", Location = new Point(320, 39), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var n = ReferenceWindowStyle.Numeric(14, 0, 365); n.SetBounds(220, 70, 70, 28); box.Controls.Add(n);
        box.Controls.Add(new Label { Text = "عدد أيام فترة قبول مرتجع البيع", Location = new Point(300, 74), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var c1 = ReferenceWindowStyle.Check("ظهور رسالة المدفوع والمتبقي مع حفظ شاشة البيع", true); c1.SetBounds(62, 106, 380, 26);
        var c2 = ReferenceWindowStyle.Check("ظهور رسالة تحذير تواريخ الصلاحية الأقرب", true); c2.SetBounds(62, 135, 380, 26);
        var c3 = ReferenceWindowStyle.Check("منع بيع منتهي الصلاحية"); c3.SetBounds(62, 164, 380, 26);
        var c4 = ReferenceWindowStyle.Check("إمكانية تغيير مخزن البيع مع كل فاتورة"); c4.SetBounds(62, 193, 380, 26);
        var cash = ReferenceWindowStyle.Combo("نقطة بيع 1"); cash.SetBounds(110, 224, 180, 28);
        box.Controls.AddRange(new Control[] { c1, c2, c3, c4, cash });
        box.Controls.Add(new Label { Text = "الكاشير الأساسي", Location = new Point(300, 228), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        _body.Controls.Add(box);
    }

    private void AddAccountingSection()
    {
        var box = Section("الحسابات", ReferenceWindowStyle.SoftLavender, 170);
        var cash = ReferenceWindowStyle.Combo("نقطة بيع 1"); cash.SetBounds(120, 38, 190, 28); box.Controls.Add(cash);
        box.Controls.Add(new Label { Text = "موقع التوريدات النقدية", Location = new Point(320, 42), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var expense = ReferenceWindowStyle.Combo("نقطة بيع 1"); expense.SetBounds(120, 78, 190, 28); box.Controls.Add(expense);
        box.Controls.Add(new Label { Text = "موقع المصروفات النقدية", Location = new Point(320, 82), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var bank = ReferenceWindowStyle.Combo("حساب بنك مصر"); bank.SetBounds(120, 118, 190, 28); box.Controls.Add(bank);
        box.Controls.Add(new Label { Text = "حساب الشيكات الصادرة", Location = new Point(320, 122), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        _body.Controls.Add(box);
    }

    private void AddGeneralSection()
    {
        var box = Section("عام", ReferenceWindowStyle.SoftLavender, 250);
        var c1 = ReferenceWindowStyle.Combo("الإنجليزية"); c1.SetBounds(150, 36, 150, 28); box.Controls.Add(c1);
        box.Controls.Add(new Label { Text = "لغة البحث الافتراضية", Location = new Point(320, 40), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var c2 = ReferenceWindowStyle.Combo("الإنجليزية"); c2.SetBounds(150, 76, 150, 28); box.Controls.Add(c2);
        box.Controls.Add(new Label { Text = "عرض الصنف في البرنامج باللغة", Location = new Point(320, 80), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var c3 = ReferenceWindowStyle.Combo("الإنجليزية"); c3.SetBounds(150, 116, 150, 28); box.Controls.Add(c3);
        box.Controls.Add(new Label { Text = "عرض الصنف داخل شاشة البحث باللغة", Location = new Point(320, 120), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var c4 = ReferenceWindowStyle.Check("التحذير لإضافة الصنف إلى شكل النواقص"); c4.SetBounds(70, 160, 370, 26); box.Controls.Add(c4);
        var c5 = ReferenceWindowStyle.Check("تشكيل النواقص مع حد الطلب", true); c5.SetBounds(70, 190, 370, 26); box.Controls.Add(c5);
        var printer = ReferenceWindowStyle.Combo("Canon MF3010"); printer.SetBounds(110, 222, 240, 28); box.Controls.Add(printer);
        box.Controls.Add(new Label { Text = "طابعة التقارير", Location = new Point(350, 226), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        _body.Controls.Add(box);
    }

    private static Panel Section(string title, Color fill, int height)
    {
        var box = new Panel { Width = 458, Height = height, BackColor = fill, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(8) };
        box.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 28, Font = ReferenceWindowStyle.Bold, TextAlign = ContentAlignment.MiddleRight });
        return box;
    }
}

public sealed class BarcodePrintSettingsForm : ReferenceWindowBase
{
    public BarcodePrintSettingsForm() : base("إعدادات طباعة الباركود", new Size(580, 590))
    {
        AddTitle("إعدادات طباعة الباركود");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.FromArgb(240, 240, 240) };
        var flags = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = ReferenceWindowStyle.SoftGreen, BorderStyle = BorderStyle.FixedSingle };
        var no = ReferenceWindowStyle.Check("لا يوجد طباعة باركود"); no.SetBounds(335, 9, 210, 26);
        var thermal = ReferenceWindowStyle.Check("طباعة على طابعة حرارية", true); thermal.SetBounds(155, 9, 180, 26);
        var a4 = ReferenceWindowStyle.Check("طباعة على طابعة A4"); a4.SetBounds(5, 9, 145, 26);
        flags.Controls.AddRange(new Control[] { no, thermal, a4 });
        var printerLabel = new Label { Text = "اسم الطابعة", Location = new Point(390, 72), AutoSize = true, Font = ReferenceWindowStyle.Bold };
        var printer = ReferenceWindowStyle.Combo("Xprinter XP-233B"); printer.SetBounds(25, 68, 350, 28);
        var orgCheck = ReferenceWindowStyle.Check("طباعة اسم المؤسسة", true); orgCheck.SetBounds(370, 112, 180, 26);
        var org = ReferenceWindowStyle.TextBox("صيدلية ياسين"); org.SetBounds(130, 110, 220, 28);
        var phoneCheck = ReferenceWindowStyle.Check("طباعة رقم التليفون", true); phoneCheck.SetBounds(370, 152, 180, 26);
        var phone = ReferenceWindowStyle.TextBox("0502338665"); phone.SetBounds(130, 150, 220, 28);
        var preview = new Label { Text = "0502338665      صيدلية ياسين\n\n||||||||||||||||||||||||||||||\nاسم الصنف مكتوب هنا       4.00 E.L.", Location = new Point(145, 190), Size = new Size(300, 90), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 10F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        var paper = new Panel { Location = new Point(145, 315), Size = new Size(300, 70), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 245, 245) };
        paper.Controls.Add(new Label { Text = "في حالة الطباعة على ورق A4", Dock = DockStyle.Top, Height = 28, Font = ReferenceWindowStyle.Bold, TextAlign = ContentAlignment.MiddleCenter });
        var paperSize = ReferenceWindowStyle.Combo("6 x 24"); paperSize.SetBounds(50, 34, 170, 28); paper.Controls.Add(paperSize);
        var close = ReferenceWindowStyle.Button("إغلاق", 90); close.SetBounds(175, 410, 90, 32); close.Click += (_, _) => Close();
        var save = ReferenceWindowStyle.Button("حفظ", 90); save.SetBounds(315, 410, 90, 32); save.Click += (_, _) => MessageBox.Show(this, "تم حفظ الإعدادات.");
        body.Controls.AddRange(new Control[] { flags, printerLabel, printer, orgCheck, org, phoneCheck, phone, preview, paper, close, save });
        Controls.Add(body);
    }
}

public sealed class SalesInvoicePrintSettingsForm : ReferenceWindowBase
{
    public SalesInvoicePrintSettingsForm() : base("إعدادات طباعة فاتورة البيع", new Size(1000, 450))
    {
        AddTitle("إعدادات طباعة فاتورة البيع");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.FromArgb(240, 240, 240) };
        var preview = new Panel { Location = new Point(12, 76), Size = new Size(275, 315), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        preview.Controls.Add(new Label { Text = "الصحة خير من الرزق\nصيدلية ابن سينا\n22334455/س\n\nاسم الصنف      الكمية   الوحدة  الإجمالي\n--------------------------------\nبروفين 200      1      شريط   21.00\nفلازول 500      1      شريط   13.00\n\nعدد القطع               2.25\nالإجمالي                51.00\nالمطلوب                51.00\n\n15/11/2015 10:34:41\n||||||||||||||||||||||||||\nأصناف الفاتورة لا ترجع", Dock = DockStyle.Fill, Font = new Font("Tahoma", 8.5F, FontStyle.Bold), TextAlign = ContentAlignment.TopCenter });
        var controls = new TableLayoutPanel { Location = new Point(310, 76), Size = new Size(675, 315), ColumnCount = 2, RowCount = 12, RightToLeft = RightToLeft.Yes };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62)); controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        AddRow(controls, "نوع الطباعة", ReferenceWindowStyle.Combo("ريستوت"), 0);
        AddRow(controls, "مقدمة الفاتورة", ReferenceWindowStyle.TextBox("تمنكم لكم الشفاء العاجل"), 1);
        AddRow(controls, "اسم المؤسسة", ReferenceWindowStyle.TextBox("صيدلية ياسين"), 2);
        AddRow(controls, "رقم التليفون", ReferenceWindowStyle.TextBox("0502338665 - 01007535159"), 3);
        AddRow(controls, "نهاية الفاتورة", ReferenceWindowStyle.TextBox("أصناف الفاتورة لا ترجع"), 4);
        AddRow(controls, "السجل الضريبي", ReferenceWindowStyle.TextBox("503502324"), 5);
        AddRow(controls, "السجل التجاري", ReferenceWindowStyle.TextBox("44669"), 6);
        AddRow(controls, "نموذج", ReferenceWindowStyle.Numeric(5, 0, 100), 7);
        AddRow(controls, "أقل قيمة للفاتورة لطباعتها", ReferenceWindowStyle.Numeric(10, 0, 100), 8);
        AddRow(controls, "عدد النسخ المطبوعة", ReferenceWindowStyle.Numeric(1, 0, 99), 9);
        AddRow(controls, "اسم الطابعة", ReferenceWindowStyle.Combo("DESKTOP-T6QU01PIBIXOLON SRP-350plus"), 10);
        AddRow(controls, "طباعة الفاتورة كما تم إدخالها", ReferenceWindowStyle.Check("طباعة الفاتورة كما تم إدخالها"), 11);
        var save = ReferenceWindowStyle.Button("حفظ"); save.SetBounds(720, 350, 95, 32); save.Click += (_, _) => MessageBox.Show(this, "تم الحفظ.");
        var close = ReferenceWindowStyle.Button("إغلاق"); close.SetBounds(545, 350, 95, 32); close.Click += (_, _) => Close();
        body.Controls.Add(preview); body.Controls.Add(controls); body.Controls.Add(save); body.Controls.Add(close); Controls.Add(body);
    }

    private static void AddRow(TableLayoutPanel panel, string label, Control control, int row)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        panel.Controls.Add(control, 0, row);
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = ReferenceWindowStyle.Bold }, 1, row);
        control.Dock = DockStyle.Fill; control.Margin = new Padding(2);
    }
}

public sealed class BackupForm : ReferenceWindowBase
{
    public BackupForm() : base("أخذ نسخة احتياطية", new Size(580, 170))
    {
        AddTitle("أخذ نسخة احتياطية");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = ReferenceWindowStyle.PaleGray };
        var path = ReferenceWindowStyle.TextBox(); path.SetBounds(80, 25, 360, 28);
        var label = new Label { Text = "مسار حفظ النسخة", Location = new Point(445, 29), AutoSize = true, Font = ReferenceWindowStyle.Bold };
        var browse = ReferenceWindowStyle.Button("استعراض", 85); browse.SetBounds(5, 22, 70, 32);
        var save = ReferenceWindowStyle.Button("موافق", 90); save.SetBounds(300, 72, 95, 32);
        save.Click += (_, _) =>
        {
            try { var p = BackupService.CreateBackup(); path.Text = p; MessageBox.Show(this, "تم إنشاء النسخة الاحتياطية."); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message); }
        };
        var close = ReferenceWindowStyle.Button("إغلاق", 90); close.SetBounds(190, 72, 95, 32); close.Click += (_, _) => Close();
        browse.Click += (_, _) => { using var d = new FolderBrowserDialog(); if (d.ShowDialog(this) == DialogResult.OK) path.Text = d.SelectedPath; };
        body.Controls.AddRange(new Control[] { label, path, browse, save, close }); Controls.Add(body);
    }
}

public sealed class ScheduledBackupForm : ReferenceWindowBase
{
    public ScheduledBackupForm() : base("النسخ الاحتياطية الدورية", new Size(620, 500))
    {
        AddTitle("النسخ الاحتياطية الدورية");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true, BackColor = Color.FromArgb(245, 245, 245) };
        var top = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(245, 245, 245) };
        AddTopButton(top, "عملية جديدة", 30); AddTopButton(top, "حفظ", 130); AddTopButton(top, "تشغيل العملية", 230); AddTopButton(top, "حذف العملية", 340); AddTopButton(top, "إغلاق", 450, true);
        var action = new Panel { Dock = DockStyle.Top, Height = 190, BackColor = ReferenceWindowStyle.SoftGreen, BorderStyle = BorderStyle.FixedSingle };
        var account = ReferenceWindowStyle.TextBox("(local)"); account.SetBounds(320, 18, 170, 28);
        var processName = ReferenceWindowStyle.TextBox(); processName.SetBounds(100, 18, 200, 28);
        action.Controls.AddRange(new Control[] { account, processName });
        action.Controls.Add(new Label { Text = "اسم العملية", Location = new Point(20, 22), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        action.Controls.Add(new Label { Text = "عملية جديدة", Location = new Point(500, 22), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var take = ReferenceWindowStyle.Numeric(3, 0, 99); take.SetBounds(280, 55, 70, 28); action.Controls.Add(take);
        var del = ReferenceWindowStyle.Numeric(3, 0, 99); del.SetBounds(90, 55, 70, 28); action.Controls.Add(del);
        action.Controls.Add(new Label { Text = "أخذ نسخة كل", Location = new Point(355, 59), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        action.Controls.Add(new Label { Text = "ساعة", Location = new Point(65, 59), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        action.Controls.Add(new Label { Text = "حذف النسخ التي من قبل", Location = new Point(170, 59), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var path = ReferenceWindowStyle.TextBox(); path.SetBounds(105, 92, 330, 28); action.Controls.Add(path);
        action.Controls.Add(new Label { Text = "مسار حفظ النسخ", Location = new Point(440, 96), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var browse = ReferenceWindowStyle.Button("استعراض", 80); browse.SetBounds(20, 90, 80, 32); action.Controls.Add(browse);
        var times = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.FromArgb(245, 245, 245) };
        var from = ReferenceWindowStyle.Combo("01:00:00 AM"); from.SetBounds(90, 10, 145, 28); times.Controls.Add(from);
        var to = ReferenceWindowStyle.Combo("11:59:00 PM"); to.SetBounds(330, 10, 145, 28); times.Controls.Add(to);
        times.Controls.Add(new Label { Text = "من الساعة", Location = new Point(245, 14), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        times.Controls.Add(new Label { Text = "إلى الساعة", Location = new Point(485, 14), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var date1 = ReferenceWindowStyle.TextBox("2026/09/05"); date1.SetBounds(90, 47, 145, 28); times.Controls.Add(date1);
        var date2 = ReferenceWindowStyle.TextBox("2026/09/05"); date2.SetBounds(330, 47, 145, 28); times.Controls.Add(date2);
        times.Controls.Add(new Label { Text = "ابتداء من يوم", Location = new Point(245, 51), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        times.Controls.Add(new Label { Text = "تاريخ الإيقاف", Location = new Point(485, 51), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var current = new TextBox { Multiline = true, ReadOnly = true, BackColor = Color.DarkGray, Size = new Size(560, 90), Dock = DockStyle.Top };
        body.Controls.Add(current); body.Controls.Add(times); body.Controls.Add(action); body.Controls.Add(top); Controls.Add(body);
    }

    private static void AddTopButton(Panel p, string text, int x, bool close = false)
    {
        var b = ReferenceWindowStyle.Button(text, 90); b.SetBounds(x, 18, 90, 32); if (close) b.Click += (_, _) => p.Parent?.Dispose(); p.Controls.Add(b);
    }
}

public sealed class DatabaseSizeForm : ReferenceWindowBase
{
    public DatabaseSizeForm() : base("حجم قاعدة البيانات", new Size(420, 185))
    {
        AddTitle("حجم قاعدة البيانات");
        var size = new Label { Text = $"MB {GetSizeMb():0.00}", Font = new Font("Tahoma", 18F, FontStyle.Bold), ForeColor = Color.Red, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(248, 248, 248) };
        Controls.Add(size);
        Controls.Add(new Label { Text = "حجم قاعدة البيانات", Dock = DockStyle.Bottom, Height = 32, Font = ReferenceWindowStyle.Bold, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 10, 0) });
    }

    private static double GetSizeMb()
    {
        try { return File.Exists(Database.DatabasePath) ? new FileInfo(Database.DatabasePath).Length / 1024d / 1024d : 0; }
        catch { return 0; }
    }
}

public sealed class BarcodePrintForm : ReferenceWindowBase
{
    public BarcodePrintForm() : base("طباعة باركود", new Size(1100, 760))
    {
        AddTitle("طباعة باركود");
        var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, RowHeadersVisible = false, AutoGenerateColumns = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, RightToLeft = RightToLeft.Yes };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "كود الصنف", Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "اسم الصنف", Width = 320 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الوحدة", Width = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "الكمية", Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "تاريخ الصلاحية", Width = 170 });
        grid.ColumnHeadersHeight = 34; grid.Font = ReferenceWindowStyle.Normal; grid.ColumnHeadersDefaultCellStyle.Font = ReferenceWindowStyle.Bold; grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(205, 220, 232);
        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(245, 245, 245) };
        var close = ReferenceWindowStyle.Button("إغلاق", 90); close.SetBounds(22, 18, 90, 32); close.Click += (_, _) => Close();
        var add = ReferenceWindowStyle.Button("إضافة صنف", 95); add.SetBounds(925, 18, 95, 32);
        var delete = ReferenceWindowStyle.Button("حذف صنف", 95); delete.SetBounds(820, 18, 95, 32);
        var print = ReferenceWindowStyle.Button("طباعة باركود", 110); print.SetBounds(690, 18, 110, 32);
        var count = new Label { Text = "0", ForeColor = Color.Red, Font = new Font("Tahoma", 12F, FontStyle.Bold), Location = new Point(575, 20), AutoSize = true };
        var countLabel = new Label { Text = "عدد الباركود", Location = new Point(455, 22), AutoSize = true, Font = ReferenceWindowStyle.Bold };
        bottom.Controls.AddRange(new Control[] { close, add, delete, print, count, countLabel }); Controls.Add(grid); Controls.Add(bottom);
    }
}

public sealed class ContractInvoiceForm : ReferenceWindowBase
{
    public ContractInvoiceForm() : base("إصدار فاتورة ورقية للتعاقد", new Size(880, 650))
    {
        AddTitle("إصدار فاتورة ورقية للتعاقد");
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = Color.FromArgb(240, 240, 240) };
        var left = new Panel { Location = new Point(10, 70), Size = new Size(210, 515), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        left.Controls.Add(new Label { Text = "الصحة خير من الرزق\nصيدلية ابن سينا\n22334455/س\n\nاسم الصنف    الكمية    الوحدة    الإجمالي\n--------------------------------\nبروفين 200   1        شريط      21.00\nترانزيفرين 500 1      شريط      13.00\n\nعدد القطع               2.25\nالإجمالي                 51.00\nالمطلوب                  51.00\n\nتاريخ الفاتورة\n15/11/2015 10:34:41\n||||||||||||||||||||||||\nأصناف الفاتورة لا ترجع", Dock = DockStyle.Fill, Font = new Font("Tahoma", 8.5F, FontStyle.Bold), TextAlign = ContentAlignment.TopCenter });
        var right = new Panel { Location = new Point(235, 70), Size = new Size(625, 515), BackColor = Color.FromArgb(240, 240, 240) };
        AddPair(right, "نوع الطباعة", ReferenceWindowStyle.Combo("ريستوت"), 40);
        AddPair(right, "مقدمة الفاتورة", ReferenceWindowStyle.TextBox("تمنكم لكم الشفاء العاجل"), 82);
        AddPair(right, "اسم المؤسسة", ReferenceWindowStyle.TextBox("صيدلية ياسين"), 124);
        AddPair(right, "رقم التليفون", ReferenceWindowStyle.TextBox("0502338665 - 01007535159"), 166);
        AddPair(right, "نهاية الفاتورة", ReferenceWindowStyle.TextBox("أصناف الفاتورة لا ترجع"), 208);
        AddPair(right, "السجل الضريبي", ReferenceWindowStyle.TextBox("503502324"), 250);
        AddPair(right, "السجل التجاري", ReferenceWindowStyle.TextBox("44669"), 292);
        var n1 = ReferenceWindowStyle.Numeric(5, 0, 99); n1.SetBounds(390, 344, 70, 28); right.Controls.Add(n1); right.Controls.Add(new Label { Text = "نموذج", Location = new Point(470, 348), AutoSize = true, ForeColor = ReferenceWindowStyle.Red, Font = ReferenceWindowStyle.Bold });
        var n2 = ReferenceWindowStyle.Numeric(10, 0, 9999); n2.SetBounds(390, 386, 70, 28); right.Controls.Add(n2); right.Controls.Add(new Label { Text = "أقل قيمة للفاتورة لطباعتها", Location = new Point(470, 390), AutoSize = true, ForeColor = ReferenceWindowStyle.Red, Font = ReferenceWindowStyle.Bold });
        var n3 = ReferenceWindowStyle.Numeric(1, 0, 99); n3.SetBounds(390, 428, 70, 28); right.Controls.Add(n3); right.Controls.Add(new Label { Text = "عدد النسخ المطبوعة", Location = new Point(470, 432), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var printer = ReferenceWindowStyle.Combo("DESKTOP-T6QU01PIBIXOLON SRP-350plus"); printer.SetBounds(70, 470, 390, 28); right.Controls.Add(printer);
        right.Controls.Add(new Label { Text = "اسم الطابعة", Location = new Point(475, 474), AutoSize = true, Font = ReferenceWindowStyle.Bold });
        var close = ReferenceWindowStyle.Button("إغلاق", 90); close.SetBounds(430, 550, 90, 32); close.Click += (_, _) => Close();
        var save = ReferenceWindowStyle.Button("حفظ", 90); save.SetBounds(290, 550, 90, 32); save.Click += (_, _) => MessageBox.Show(this, "تم حفظ الإعدادات.");
        body.Controls.Add(left); body.Controls.Add(right); body.Controls.Add(save); body.Controls.Add(close); Controls.Add(body);
    }

    private static void AddPair(Panel p, string label, Control control, int y)
    {
        var l = new Label { Text = label, Location = new Point(500, y + 4), AutoSize = true, Font = ReferenceWindowStyle.Bold };
        control.SetBounds(80, y, 390, 28); p.Controls.Add(control); p.Controls.Add(l);
    }
}
