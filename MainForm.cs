using System;
using System.Drawing;
using System.Windows.Forms;
using GlowvaERP.Forms;
using GlowvaERP.Helpers;

namespace GlowvaERP;

public sealed class MainForm : Form
{
    private readonly MenuStrip _menu;
    private readonly Panel _content;

    public MainForm()
    {
        Text = "Glowva ERP";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1200, 700);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(245, 245, 245);
        MainMenuStrip = _menu = BuildMenu();

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 248, 248),
            Padding = new Padding(24),
            RightToLeft = RightToLeft.Yes
        };

        Controls.Add(_content);
        Controls.Add(_menu);
        BuildDashboard();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top,
            RightToLeft = RightToLeft.Yes,
            GripStyle = ToolStripGripStyle.Hidden,
            RenderMode = ToolStripRenderMode.System,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Padding = new Padding(8, 4, 8, 4)
        };

        menu.Items.Add(CreateMenu("الرئيسية",
            Item("لوحة التحكم", (_, _) => BuildDashboard(true)),
            Item("تحديث الشاشة", (_, _) => RefreshDashboard()),
            new ToolStripSeparator(),
            Item("خروج", (_, _) => Close())));

        // كل ما يخص المبيعات، بما فيه تقارير المبيعات، داخل قائمة المبيعات.
        menu.Items.Add(CreateMenu("المبيعات",
            Item("فاتورة بيع جديدة", (_, _) => OpenForm<SalesForm>()),
            Item("استدعاء فاتورة", (_, _) => ShowInvoiceSearch(true)),
            Item("مرتجعات المبيعات", (_, _) => OpenForm<SalesReturnsForm>()),
            new ToolStripSeparator(),
            CreateMenu("تقارير المبيعات",
                Item("تقرير المبيعات", (_, _) => OpenForm<SalesReportForm>()),
                Item("تقرير مرتجعات المبيعات", (_, _) => ShowNotReady("تقرير مرتجعات المبيعات"))),
            Item("العملاء", (_, _) => OpenForm(() => new PartyListForm(false)))));

        // كل ما يخص المشتريات، بما فيه تقارير المشتريات، داخل قائمة المشتريات.
        menu.Items.Add(CreateMenu("المشتريات",
            Item("فاتورة شراء جديدة", (_, _) => OpenForm<PurchasesForm>()),
            Item("استدعاء فاتورة", (_, _) => ShowInvoiceSearch(false)),
            Item("مرتجعات المشتريات", (_, _) => OpenForm<PurchaseReturnsForm>()),
            new ToolStripSeparator(),
            CreateMenu("تقارير المشتريات",
                Item("تقرير المشتريات", (_, _) => ShowNotReady("تقرير المشتريات")),
                Item("تقرير مرتجعات المشتريات", (_, _) => ShowNotReady("تقرير مرتجعات المشتريات"))),
            Item("الموردين", (_, _) => OpenForm(() => new PartyListForm(true)))));

        // وظائف الأصناف والمخازن وتقاريرها مرتبطة بالقسم نفسه.
        menu.Items.Add(CreateMenu("الأصناف",
            Item("الأصناف", (_, _) => OpenForm<ProductsForm>()),
            Item("بحث الأصناف", (_, _) => ShowProductSearch()),
            Item("كارت الصنف", (_, _) => ShowProductCard()),
            new ToolStripSeparator(),
            CreateMenu("تقارير الأصناف",
                Item("تقرير الأصناف", (_, _) => ShowNotReady("تقرير الأصناف")),
                Item("تقرير حركة الصنف", (_, _) => ShowNotReady("تقرير حركة الصنف")))));

        menu.Items.Add(CreateMenu("المخازن",
            Item("المخزون", (_, _) => OpenForm<InventoryForm>()),
            new ToolStripSeparator(),
            CreateMenu("تقارير المخازن",
                Item("تقرير المخزون", (_, _) => ShowNotReady("تقرير المخزون")),
                Item("الأصناف الناقصة", (_, _) => ShowNotReady("الأصناف الناقصة")),
                Item("حركة المخزون", (_, _) => ShowNotReady("حركة المخزون")))));

        menu.Items.Add(CreateMenu("الحسابات",
            Item("حسابات العملاء والموردين", (_, _) => OpenForm<AccountsForm>()),
            Item("الخزينة", (_, _) => OpenForm<CashForm>()),
            Item("المصروفات", (_, _) => ShowNotReady("شاشة المصروفات")),
            new ToolStripSeparator(),
            CreateMenu("تقارير الحسابات",
                Item("كشف حساب العملاء", (_, _) => ShowNotReady("كشف حساب العملاء")),
                Item("كشف حساب الموردين", (_, _) => ShowNotReady("كشف حساب الموردين")),
                Item("تقرير الخزينة", (_, _) => ShowNotReady("تقرير الخزينة")))));

        menu.Items.Add(CreateMenu("الإعدادات",
            Item("استيراد البيانات القديمة", (_, _) => OpenForm<LegacyImportForm>()),
            Item("إعدادات البرنامج", (_, _) => ShowNotReady("إعدادات البرنامج"))));

        return menu;
    }

    private void ShowInvoiceSearch(bool salesMode)
    {
        using var dialog = new InvoiceSearchForm(salesMode);
        dialog.ShowDialog(this);
    }

    private static ToolStripMenuItem CreateMenu(string text, params ToolStripItem[] children)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(10, 4, 10, 4)
        };
        item.DropDownItems.AddRange(children);
        foreach (ToolStripItem child in item.DropDownItems)
            child.RightToLeft = RightToLeft.Yes;
        return item;
    }

    private static ToolStripMenuItem Item(string text, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 5, 8, 5)
        };
        item.Click += handler;
        return item;
    }

    private void OpenForm<T>() where T : Form, new()
    {
        using var form = new T();
        AttachContextActions(form);
        form.ShowDialog(this);
    }

    private void OpenForm<T>(Func<T> factory) where T : Form
    {
        using var form = factory();
        AttachContextActions(form);
        form.ShowDialog(this);
    }

    private void AttachContextActions(Form form)
    {
        void SearchProducts(Form owner)
        {
            using var dialog = new ProductSearchDialog();
            dialog.ShowDialog(owner);
        }

        void NewSales(Form owner)
        {
            using var next = new SalesForm();
            AttachContextActions(next);
            next.ShowDialog(owner);
            owner.Close();
        }

        void NewPurchases(Form owner)
        {
            using var next = new PurchasesForm();
            AttachContextActions(next);
            next.ShowDialog(owner);
            owner.Close();
        }

        void SaveCurrent(Form owner)
        {
            if (owner is SalesForm sales)
                sales.TriggerSave();
            else if (owner is PurchasesForm purchases)
                purchases.TriggerSave();
        }

        void FocusProduct(Form owner)
        {
            if (owner is SalesForm sales)
                sales.FocusProductEntry();
            else if (owner is PurchasesForm purchases)
                purchases.FocusProductEntry();
        }

        void AddLine(Form owner)
        {
            if (owner is SalesForm sales)
                sales.AddLine();
            else if (owner is PurchasesForm purchases)
                purchases.AddLine();
        }

        void DeleteLine(Form owner)
        {
            if (owner is SalesForm sales)
                sales.DeleteSelectedLine();
            else if (owner is PurchasesForm purchases)
                purchases.DeleteSelectedLine();
        }

        void ShowNotReadyFrom(Form owner, string label)
        {
            MessageBox.Show(owner, $"{label} ستكون مرتبطة بنفس دورة العمل بعد بناء شاشة {label} بالكامل.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if (form is SalesForm)
        {
            ContextualSidebar.Attach(form, "المبيعات",
                new("جديد", NewSales, Color.FromArgb(46, 125, 50)),
                new("حفظ", SaveCurrent, Color.FromArgb(33, 150, 243)),
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("إضافة صنف", FocusProduct, Color.FromArgb(0, 121, 107)),
                new("سطر جديد", AddLine, Color.FromArgb(0, 121, 107)),
                new("حذف سطر", DeleteLine, Color.FromArgb(211, 47, 47)),
                new("فواتير مرتجعة", f => OpenForm<SalesReturnsForm>(), Color.FromArgb(239, 125, 34)),
                new("العملاء", f => OpenForm(() => new PartyListForm(false)), Color.FromArgb(66, 133, 244)),
                new("فواتير غير مكتملة", f => ShowNotReadyFrom(f, "فواتير غير مكتملة"), Color.FromArgb(117, 117, 117)),
                new("طباعة المستخدم", f => ShowNotReadyFrom(f, "طباعة المستخدم"), Color.FromArgb(117, 117, 117)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is PurchasesForm)
        {
            ContextualSidebar.Attach(form, "المشتريات",
                new("جديد", NewPurchases, Color.FromArgb(46, 125, 50)),
                new("حفظ", SaveCurrent, Color.FromArgb(33, 150, 243)),
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("إضافة صنف", FocusProduct, Color.FromArgb(0, 121, 107)),
                new("سطر جديد", AddLine, Color.FromArgb(0, 121, 107)),
                new("حذف سطر", DeleteLine, Color.FromArgb(211, 47, 47)),
                new("فواتير مرتجعة", f => OpenForm<PurchaseReturnsForm>(), Color.FromArgb(239, 125, 34)),
                new("الموردين", f => OpenForm(() => new PartyListForm(true)), Color.FromArgb(66, 133, 244)),
                new("فواتير غير مكتملة", f => ShowNotReadyFrom(f, "فواتير غير مكتملة"), Color.FromArgb(117, 117, 117)),
                new("طباعة المستخدم", f => ShowNotReadyFrom(f, "طباعة المستخدم"), Color.FromArgb(117, 117, 117)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is SalesReturnsForm)
        {
            ContextualSidebar.Attach(form, "مرتجعات المبيعات",
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("المبيعات", f => OpenForm<SalesForm>(), Color.FromArgb(46, 125, 50)),
                new("العملاء", f => OpenForm(() => new PartyListForm(false)), Color.FromArgb(66, 133, 244)),
                new("حفظ", f => ShowNotReadyFrom(f, "حفظ المرتجع"), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is PurchaseReturnsForm)
        {
            ContextualSidebar.Attach(form, "مرتجعات المشتريات",
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("المشتريات", f => OpenForm<PurchasesForm>(), Color.FromArgb(46, 125, 50)),
                new("الموردين", f => OpenForm(() => new PartyListForm(true)), Color.FromArgb(66, 133, 244)),
                new("حفظ", f => ShowNotReadyFrom(f, "حفظ المرتجع"), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is ProductsForm)
        {
            ContextualSidebar.Attach(form, "الأصناف",
                new("جديد", f => { }, Color.FromArgb(46, 125, 50)),
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("المخزون", f => OpenForm<InventoryForm>(), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is InventoryForm)
        {
            ContextualSidebar.Attach(form, "المخزون",
                new("بحث الأصناف", SearchProducts, Color.FromArgb(33, 150, 243)),
                new("الأصناف", f => OpenForm<ProductsForm>(), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is AccountsForm)
        {
            ContextualSidebar.Attach(form, "الحسابات",
                new("العملاء", f => OpenForm(() => new PartyListForm(false)), Color.FromArgb(66, 133, 244)),
                new("الموردين", f => OpenForm(() => new PartyListForm(true)), Color.FromArgb(66, 133, 244)),
                new("الخزينة", f => OpenForm<CashForm>(), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is CashForm)
        {
            ContextualSidebar.Attach(form, "الخزينة",
                new("الحسابات", f => OpenForm<AccountsForm>(), Color.FromArgb(66, 133, 244)),
                new("المبيعات", f => OpenForm<SalesForm>(), Color.FromArgb(33, 150, 243)),
                new("المشتريات", f => OpenForm<PurchasesForm>(), Color.FromArgb(33, 150, 243)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else if (form is PartyListForm)
        {
            ContextualSidebar.Attach(form, "بيانات العملاء / الموردين",
                new("المبيعات", f => OpenForm<SalesForm>(), Color.FromArgb(33, 150, 243)),
                new("المشتريات", f => OpenForm<PurchasesForm>(), Color.FromArgb(33, 150, 243)),
                new("الحسابات", f => OpenForm<AccountsForm>(), Color.FromArgb(46, 125, 50)),
                new("الخزينة", f => OpenForm<CashForm>(), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
        else
        {
            ContextualSidebar.Attach(form, form.Text,
                new("لوحة التحكم", f => CloseAndRefresh(f), Color.FromArgb(33, 150, 243)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        }
    }

    private void CloseAndRefresh(Form form)
    {
        form.Close();
        BuildDashboard(true);
    }

    private void ShowProductSearch()
    {
        using var dialog = new ProductSearchDialog();
        ContextualSidebar.Attach(dialog, "بحث الأصناف",
            new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
        dialog.ShowDialog(this);
    }

    private void ShowProductCard()
    {
        using var dialog = new ProductSearchDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedProductId is > 0)
        {
            using var card = new ProductCardDialog(dialog.SelectedProductId.Value);
            ContextualSidebar.Attach(card, "كارت الصنف",
                new("بحث الأصناف", f => ShowProductSearchFrom(f), Color.FromArgb(33, 150, 243)),
                new("المخزون", f => OpenForm<InventoryForm>(), Color.FromArgb(46, 125, 50)),
                new("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
            card.ShowDialog(this);
        }
    }

    private void ShowProductSearchFrom(Form owner)
    {
        using var dialog = new ProductSearchDialog();
        dialog.ShowDialog(owner);
    }

    private void ShowNotReady(string name)
    {
        MessageBox.Show(this, $"{name} سيتم تجهيزها في المرحلة التالية.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshDashboard()
    {
        BuildDashboard(true);
    }

    private void BuildDashboard(bool clearFirst = false)
    {
        if (clearFirst)
            _content.Controls.Clear();

        var heading = new Label
        {
            Text = "لوحة التحكم",
            Dock = DockStyle.Top,
            Height = 60,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 220,
            ColumnCount = 3,
            RowCount = 2,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(0, 15, 0, 15)
        };

        for (int c = 0; c < 3; c++)
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        for (int r = 0; r < 2; r++)
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        AddCard(cards, "مبيعات اليوم", "0.00", 0, 0);
        AddCard(cards, "مشتريات اليوم", "0.00", 1, 0);
        AddCard(cards, "صافي الربح", "0.00", 2, 0);
        AddCard(cards, "رصيد العملاء", "0.00", 0, 1);
        AddCard(cards, "رصيد الموردين", "0.00", 1, 1);
        AddCard(cards, "أصناف منخفضة", "0", 2, 1);

        _content.Controls.Add(cards);
        _content.Controls.Add(heading);
        heading.BringToFront();
    }

    private static void AddCard(TableLayoutPanel parent, string title, string value, int col, int row)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            BackColor = Color.Gainsboro,
            RightToLeft = RightToLeft.Yes
        };
        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9),
            RightToLeft = RightToLeft.Yes
        };
        var valueLabel = new Label
        {
            Text = value,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 88, 160),
            RightToLeft = RightToLeft.Yes
        };
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(titleLabel);
        parent.Controls.Add(panel, col, row);
    }
}