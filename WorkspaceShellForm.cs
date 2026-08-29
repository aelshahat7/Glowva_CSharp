using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Forms;
using GlowvaERP.Helpers;

namespace GlowvaERP;

public sealed class WorkspaceShellForm : Form
{
    private readonly Panel _documentHost;
    private readonly Label _activeModule;
    private readonly Button[] _moduleButtons;
    private Form? _activeForm;

    public WorkspaceShellForm()
    {
        Text = "Glowva ERP";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 720);
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        KeyPreview = true;

        var menu = BuildMenu();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.No,
            BackColor = Color.White
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));

        var work = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.White
        };
        work.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        work.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _activeModule = new Label
        {
            Dock = DockStyle.Fill,
            Text = "الصفحة الرئيسية",
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(10, 0, 14, 0),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 60),
            BackColor = Color.FromArgb(245, 245, 245),
            RightToLeft = RightToLeft.Yes
        };

        _documentHost = new Panel
        {
            Name = "DocumentHost",
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoScroll = false
        };

        work.Controls.Add(_activeModule, 0, 0);
        work.Controls.Add(_documentHost, 0, 1);

        var rightRail = BuildRightRail(out _moduleButtons);

        root.Controls.Add(work, 0, 0);
        root.Controls.Add(rightRail, 1, 0);

        Controls.Add(root);
        Controls.Add(menu);
        MainMenuStrip = menu;

        ShowWelcome();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top,
            Height = 38,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = UiTheme.ChromeGold,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(8, 2, 8, 2),
            RightToLeft = RightToLeft.Yes,
            Renderer = new ShellMenuRenderer()
        };

        menu.Items.Add(CreateMenu("البيانات العامة",
            Item("الترحيب", (_, _) => ShowWelcome()),
            Item("خروج", (_, _) => Close())));

        menu.Items.Add(CreateMenu("الأصناف",
            Item("الأصناف", (_, _) => OpenEmbedded<ProductsForm>("الأصناف")),
            Item("بحث الأصناف", (_, _) => OpenEmbedded<ProductSearchDialog>("بحث الأصناف"))));

        menu.Items.Add(CreateMenu("المخازن",
            Item("المخزون", (_, _) => OpenEmbedded<InventoryForm>("المخزون"))));

        menu.Items.Add(CreateMenu("الموردين",
            Item("بيانات الموردين", (_, _) => OpenEmbedded(() => new PartyListForm(true), "الموردين")),
            Item("المشتريات", (_, _) => OpenEmbedded<PurchasesForm>("المشتريات"))));

        menu.Items.Add(CreateMenu("المشتريات",
            Item("فاتورة شراء", (_, _) => OpenEmbedded<PurchasesForm>("المشتريات")),
            Item("مرتجعات المشتريات", (_, _) => OpenEmbedded<PurchaseReturnsForm>("مرتجعات المشتريات"))));

        menu.Items.Add(CreateMenu("العملاء",
            Item("بيانات العملاء", (_, _) => OpenEmbedded(() => new PartyListForm(false), "العملاء")),
            Item("الحسابات", (_, _) => OpenEmbedded<AccountsForm>("الحسابات"))));

        menu.Items.Add(CreateMenu("المبيعات",
            Item("فاتورة بيع", (_, _) => OpenEmbedded<SalesForm>("المبيعات")),
            Item("استدعاء فاتورة", (_, _) => OpenEmbedded(() => new InvoiceSearchForm(true), "استدعاء فاتورة")),
            Item("مرتجعات المبيعات", (_, _) => OpenEmbedded<SalesReturnsForm>("مرتجعات المبيعات")),
            new ToolStripSeparator(),
            Item("تقرير المبيعات", (_, _) => OpenEmbedded<SalesReportForm>("تقرير المبيعات"))));

        menu.Items.Add(CreateMenu("الحسابات اليومية",
            Item("الخزينة", (_, _) => OpenEmbedded<CashForm>("الخزينة")),
            Item("الحسابات", (_, _) => OpenEmbedded<AccountsForm>("الحسابات"))));

        menu.Items.Add(CreateMenu("الإيصالات",
            Item("المصروفات", (_, _) => ShowInfo("شاشة المصروفات"))));

        menu.Items.Add(CreateMenu("الأطباء", Item("الأطباء", (_, _) => ShowInfo("قسم الأطباء"))));
        menu.Items.Add(CreateMenu("شئون العاملين", Item("شئون العاملين", (_, _) => ShowInfo("قسم شئون العاملين"))));
        menu.Items.Add(CreateMenu("إطار", Item("تغيير الحجم", (_, _) => ToggleWindowState())));

        return menu;
    }

    private Panel BuildRightRail(out Button[] buttons)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.ChromeGold,
            Padding = new Padding(4),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes
        };

        var items = new[]
        {
            ("الأصناف", "💊", Color.FromArgb(33, 150, 243), (Action)(() => OpenEmbedded<ProductsForm>("الأصناف"))),
            ("المخازن", "🏬", Color.FromArgb(25, 118, 210), (Action)(() => OpenEmbedded<InventoryForm>("المخازن"))),
            ("المشتريات", "🛒", Color.FromArgb(123, 31, 162), (Action)(() => OpenEmbedded<PurchasesForm>("المشتريات"))),
            ("المبيعات", "🛒", Color.FromArgb(30, 136, 229), (Action)(() => OpenEmbedded<SalesForm>("المبيعات"))),
            ("العملاء", "👥", Color.FromArgb(41, 128, 185), (Action)(() => OpenEmbedded(() => new PartyListForm(false), "العملاء")))
        };

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = items.Length,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = UiTheme.ChromeGold
        };
        for (var i = 0; i < items.Length; i++)
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / items.Length));

        buttons = new Button[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var index = i;
            var button = new Button
            {
                Name = "ModuleButton" + i,
                Text = $"{item.Item2}\r\n{item.Item1}",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = item.Item3,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) =>
            {
                SelectModule(index);
                item.Item4();
            };
            buttons[i] = button;
            host.Controls.Add(button, 0, i);
        }

        panel.Controls.Add(host);
        return panel;
    }

    private void SelectModule(int index)
    {
        for (var i = 0; i < _moduleButtons.Length; i++)
            _moduleButtons[i].FlatAppearance.BorderSize = i == index ? 2 : 0;
        for (var i = 0; i < _moduleButtons.Length; i++)
            _moduleButtons[i].FlatAppearance.BorderColor = Color.White;
    }

    private void OpenEmbedded<T>(string title) where T : Form, new()
    {
        OpenEmbedded(() => new T(), title);
    }

    private void OpenEmbedded<T>(Func<T> factory, string title) where T : Form
    {
        if (_activeForm != null)
        {
            _activeForm.Hide();
            _activeForm.Dispose();
            _activeForm = null;
        }

        var form = factory();
        form.Text = title;
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.Margin = Padding.Empty;
        form.Padding = Padding.Empty;
        form.ShowInTaskbar = false;
        form.Parent = _documentHost;
        _documentHost.Controls.Add(form);
        _activeForm = form;
        _activeModule.Text = title;
        form.Show();
        form.BringToFront();
    }

    private void ShowWelcome()
    {
        if (_activeForm != null)
        {
            _activeForm.Hide();
            _activeForm.Dispose();
            _activeForm = null;
        }

        var welcome = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(244, 247, 249),
            Padding = new Padding(60),
            RightToLeft = RightToLeft.Yes
        };

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(40),
            BackColor = Color.White,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            RightToLeft = RightToLeft.Yes
        };
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 22F));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));

        var logo = new Label
        {
            Text = "GLOWVA ERP",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 28F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 82, 156)
        };

        var welcomeTitle = new Label
        {
            Text = "مرحبًا بك في نظام إدارة الصيدلية",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 40, 40)
        };

        var subtitle = new Label
        {
            Text = "إدارة المبيعات والمشتريات والمخزون والحسابات من مكان واحد",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.FromArgb(90, 90, 90)
        };

        var shortcuts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(30, 10, 30, 10),
            RightToLeft = RightToLeft.Yes
        };
        for (var i = 0; i < 3; i++)
            shortcuts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));

        AddWelcomeCard(shortcuts, 0, "المبيعات", "فتح فاتورة بيع جديدة", Color.FromArgb(30, 136, 229), () => OpenEmbedded<SalesForm>("المبيعات"));
        AddWelcomeCard(shortcuts, 1, "المشتريات", "فتح فاتورة شراء جديدة", Color.FromArgb(123, 31, 162), () => OpenEmbedded<PurchasesForm>("المشتريات"));
        AddWelcomeCard(shortcuts, 2, "الأصناف", "إدارة الأصناف", Color.FromArgb(33, 150, 243), () => OpenEmbedded<ProductsForm>("الأصناف"));

        var hint = new Label
        {
            Text = "استخدم شريط القوائم بالأعلى أو أزرار الأقسام على اليمين للبدء",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = Color.FromArgb(100, 100, 100)
        };

        card.Controls.Add(logo, 0, 0);
        card.Controls.Add(welcomeTitle, 0, 1);
        card.Controls.Add(subtitle, 0, 2);
        card.Controls.Add(shortcuts, 0, 3);
        card.Controls.Add(hint, 0, 4);
        welcome.Controls.Add(card);
        _documentHost.Controls.Add(welcome);
        welcome.BringToFront();
        _activeModule.Text = "الصفحة الرئيسية";
    }

    private static void AddWelcomeCard(TableLayoutPanel parent, int col, string title, string subtitle, Color color, Action action)
    {
        var button = new Button
        {
            Text = $"{title}\r\n{subtitle}",
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        parent.Controls.Add(button, col, 0);
    }

    private static ToolStripMenuItem CreateMenu(string text, params ToolStripItem[] children)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(8, 4, 8, 4)
        };
        item.DropDownItems.AddRange(children);
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

    private void ToggleWindowState()
    {
        WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
    }

    private static void ShowInfo(string text)
    {
        MessageBox.Show(text, "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            if (_activeForm != null)
            {
                _activeForm.Hide();
                _activeForm.Dispose();
                _activeForm = null;
                ShowWelcome();
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private sealed class ShellMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(UiTheme.ChromeGold);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }
    }
}
