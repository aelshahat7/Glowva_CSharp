using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Forms;
using GlowvaERP.Helpers;

namespace GlowvaERP;

public sealed class WorkspaceShellForm : Form
{
    private readonly MdiClient? _mdiClient;
    private readonly Button[] _moduleButtons;
    private readonly ToolStripMenuItem _windowMenu;
    private readonly Panel _rightRail;

    private static readonly Color[] ModuleColors =
    {
        Color.FromArgb(33, 150, 243),
        Color.FromArgb(25, 118, 210),
        Color.FromArgb(123, 31, 162),
        Color.FromArgb(30, 136, 229),
        Color.FromArgb(41, 128, 185)
    };

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
        IsMdiContainer = true;
        AutoScroll = false;

        var menu = BuildMenu(out _windowMenu);
        MainMenuStrip = menu;
        Controls.Add(menu);

        _rightRail = BuildRightRail(out _moduleButtons);
        Controls.Add(_rightRail);
        _rightRail.BringToFront();

        _mdiClient = Controls.OfType<MdiClient>().FirstOrDefault();
        if (_mdiClient != null)
        {
            _mdiClient.BackColor = Color.White;
            _mdiClient.BorderStyle = BorderStyle.None;
            _mdiClient.SizeChanged += (_, _) => LayoutMdiClient();
            _mdiClient.ControlAdded += (_, _) => RefreshWindowMenu();
            _mdiClient.ControlRemoved += (_, _) => RefreshWindowMenu();
        }

        Resize += (_, _) => LayoutMdiClient();
        MdiChildActivate += (_, _) => RefreshWindowMenu();
        FormClosed += (_, _) =>
        {
            foreach (var child in MdiChildren)
                child.FormClosed -= ChildClosed;
        };

        LayoutMdiClient();
        RefreshWindowMenu();
    }

    private MenuStrip BuildMenu(out ToolStripMenuItem windowMenu)
    {
        windowMenu = new ToolStripMenuItem("إطار")
        {
            RightToLeft = RightToLeft.Yes,
            Name = "WindowMenu"
        };
        windowMenu.DropDownOpening += (_, _) => RefreshWindowMenu();

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
            Item("الترحيب", (_, _) => ShowWorkspace()),
            Item("خروج", (_, _) => Close())));

        menu.Items.Add(CreateMenu("الأصناف",
            Item("الأصناف", (_, _) => OpenChild(() => new ProductsForm(), "الأصناف")),
            Item("بحث الأصناف", (_, _) => OpenChild(() => new ProductSearchDialog(), "بحث الأصناف"))));

        menu.Items.Add(CreateMenu("المخازن",
            Item("المخزون", (_, _) => OpenChild(() => new InventoryForm(), "المخزون"))));

        menu.Items.Add(CreateMenu("الموردين",
            Item("بيانات الموردين", (_, _) => OpenChild(() => new PartyListForm(true), "الموردين")),
            Item("المشتريات", (_, _) => OpenChild(() => new PurchasesForm(), "المشتريات"))));

        menu.Items.Add(CreateMenu("المشتريات",
            Item("فاتورة شراء", (_, _) => OpenChild(() => new PurchasesForm(), "المشتريات")),
            Item("مرتجعات المشتريات", (_, _) => OpenChild(() => new PurchaseReturnsForm(), "مرتجعات المشتريات"))));

        menu.Items.Add(CreateMenu("العملاء",
            Item("بيانات العملاء", (_, _) => OpenChild(() => new PartyListForm(false), "العملاء")),
            Item("الحسابات", (_, _) => OpenChild(() => new AccountsForm(), "الحسابات"))));

        menu.Items.Add(CreateMenu("المبيعات",
            Item("فاتورة بيع", (_, _) => OpenChild(() => new SalesForm(), "المبيعات")),
            Item("استدعاء فاتورة", (_, _) => OpenChild(() => new InvoiceSearchForm(true), "استدعاء فاتورة")),
            Item("مرتجعات المبيعات", (_, _) => OpenChild(() => new SalesReturnsForm(), "مرتجعات المبيعات")),
            new ToolStripSeparator(),
            Item("تقرير المبيعات", (_, _) => OpenChild(() => new SalesReportForm(), "تقرير المبيعات"))));

        menu.Items.Add(CreateMenu("الحسابات اليومية",
            Item("الخزينة", (_, _) => OpenChild(() => new CashForm(), "الخزينة")),
            Item("الحسابات", (_, _) => OpenChild(() => new AccountsForm(), "الحسابات"))));

        menu.Items.Add(CreateMenu("الإيصالات",
            Item("المصروفات", (_, _) => OpenChild(() => new ExpensesForm(), "المصروفات"))));

        menu.Items.Add(CreateMenu("الأطباء",
            Item("الأطباء", (_, _) => ShowInfo("قسم الأطباء"))));

        menu.Items.Add(CreateMenu("شئون العاملين",
            Item("شئون العاملين", (_, _) => ShowInfo("قسم شئون العاملين"))));

        menu.Items.Add(windowMenu);
        return menu;
    }

    private Panel BuildRightRail(out Button[] buttons)
    {
        var panel = new Panel
        {
            Dock = DockStyle.None,
            Width = 64,
            BackColor = UiTheme.ChromeGold,
            Padding = new Padding(2, 0, 2, 0),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes
        };

        var items = new[]
        {
            ("الأصناف", "💊", (Action)(() => OpenChild(() => new ProductsForm(), "الأصناف"))),
            ("المخازن", "🏬", (Action)(() => OpenChild(() => new InventoryForm(), "المخازن"))),
            ("المشتريات", "🛒", (Action)(() => OpenChild(() => new PurchasesForm(), "المشتريات"))),
            ("المبيعات", "🛒", (Action)(() => OpenChild(() => new SalesForm(), "المبيعات"))),
            ("العملاء", "👥", (Action)(() => OpenChild(() => new PartyListForm(false), "العملاء")))
        };

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = items.Length,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.ChromeGold,
            RightToLeft = RightToLeft.Yes
        };

        for (var i = 0; i < items.Length; i++)
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / items.Length));

        buttons = new Button[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Name = $"ModuleButton{index}",
                Text = $"{items[i].Item2}\r\n{items[i].Item1}",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 1, 0, 1),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = ModuleColors[index],
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                RightToLeft = RightToLeft.Yes,
                TabStop = false,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ModuleColors[index];
            button.FlatAppearance.MouseDownBackColor = ModuleColors[index];
            button.FlatAppearance.CheckedBackColor = ModuleColors[index];
            button.Click += (_, _) =>
            {
                SelectModule(index);
                items[index].Item3();
            };
            buttons[index] = button;
            host.Controls.Add(button, 0, index);
        }

        panel.Controls.Add(host);
        return panel;
    }

    private void LayoutMdiClient()
    {
        if (_mdiClient == null || _rightRail.IsDisposed)
            return;

        var top = MainMenuStrip?.Bottom ?? 0;
        var right = _rightRail.Width;
        var width = Math.Max(0, ClientSize.Width - right);
        var height = Math.Max(0, ClientSize.Height - top);

        _mdiClient.Dock = DockStyle.None;
        _mdiClient.SetBounds(0, top, width, height);
        _rightRail.SetBounds(width, top, right, height);
        _rightRail.BringToFront();
    }

    private void SelectModule(int index)
    {
        for (var i = 0; i < _moduleButtons.Length; i++)
        {
            var color = ModuleColors[i];
            _moduleButtons[i].BackColor = color;
            _moduleButtons[i].FlatAppearance.MouseOverBackColor = color;
            _moduleButtons[i].FlatAppearance.MouseDownBackColor = color;
            _moduleButtons[i].FlatAppearance.CheckedBackColor = color;
            _moduleButtons[i].FlatAppearance.BorderSize = i == index ? 2 : 0;
            _moduleButtons[i].FlatAppearance.BorderColor = Color.White;
        }
    }

    public Form? OpenChild<T>(Func<T> factory, string title) where T : Form
    {
        var form = factory();
        form.Text = title;
        form.MdiParent = this;
        form.StartPosition = FormStartPosition.Manual;
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.MinimizeBox = true;
        form.MaximizeBox = true;
        form.ControlBox = true;
        form.ShowInTaskbar = false;
        form.FormClosed += ChildClosed;

        var childCount = MdiChildren.Length;
        var clientWidth = _mdiClient?.ClientSize.Width ?? 1100;
        var clientHeight = _mdiClient?.ClientSize.Height ?? 700;

        form.WindowState = FormWindowState.Normal;
        form.Size = new Size(
            Math.Max(900, Math.Min(clientWidth - 40, 1060)),
            Math.Max(620, Math.Min(clientHeight - 40, 650)));

        var offset = Math.Min(childCount, 5) * 24;
        form.Location = new Point(20 + offset, 20 + offset);

        form.Show();
        form.Activate();
        RefreshWindowMenu();
        return form;
    }

    private void ChildClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is Form form)
            form.FormClosed -= ChildClosed;
        RefreshWindowMenu();
    }

    private void RefreshWindowMenu()
    {
        _windowMenu.DropDownItems.Clear();

        var desktop = new ToolStripMenuItem("عرض البرنامج")
        {
            RightToLeft = RightToLeft.Yes,
            CheckOnClick = false
        };
        desktop.Click += (_, _) =>
        {
            _mdiClient?.Focus();
            Activate();
        };
        _windowMenu.DropDownItems.Add(desktop);

        var children = MdiChildren.Where(f => !f.IsDisposed).ToArray();
        if (children.Length == 0)
            return;

        _windowMenu.DropDownItems.Add(new ToolStripSeparator());

        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var item = new ToolStripMenuItem($"{i + 1} {child.Text}")
            {
                RightToLeft = RightToLeft.Yes,
                CheckOnClick = false,
                Checked = ReferenceEquals(child, ActiveMdiChild),
                Tag = child
            };
            item.Click += (_, _) =>
            {
                if (item.Tag is Form target && !target.IsDisposed)
                {
                    if (target.WindowState == FormWindowState.Minimized)
                        target.WindowState = FormWindowState.Normal;
                    target.Activate();
                    target.BringToFront();
                    RefreshWindowMenu();
                }
            };
            _windowMenu.DropDownItems.Add(item);
        }
    }

    private void ShowWorkspace()
    {
        _mdiClient?.Focus();
        Activate();
    }

    private static ToolStripMenuItem CreateMenu(string text, params ToolStripItem[] children)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleCenter
        };
        item.DropDownItems.AddRange(children);
        return item;
    }

    private static ToolStripMenuItem Item(string text, EventHandler click)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes
        };
        item.Click += click;
        return item;
    }

    private void ShowInfo(string text)
    {
        MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if ((keyData & Keys.Control) == Keys.Control &&
            (keyData & Keys.Tab) == Keys.Tab)
        {
            var reverse = (keyData & Keys.Shift) == Keys.Shift;
            SwitchMdiChild(reverse);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SwitchMdiChild(bool reverse)
    {
        var children = MdiChildren.Where(f => !f.IsDisposed).ToArray();
        if (children.Length == 0)
            return;
        if (children.Length == 1)
        {
            children[0].Activate();
            return;
        }

        var current = Array.IndexOf(children, ActiveMdiChild);
        if (current < 0)
            current = reverse ? 0 : children.Length - 1;

        var next = reverse
            ? (current - 1 + children.Length) % children.Length
            : (current + 1) % children.Length;

        children[next].Activate();
        children[next].BringToFront();
        RefreshWindowMenu();
    }
}
