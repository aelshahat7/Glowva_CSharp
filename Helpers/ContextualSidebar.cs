using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

public static class ContextualSidebar
{
    private const int LeftRailWidth = 92;
    private const int RightRailWidth = 82;
    private const int MenuHeight = 38;
    private const int StatusHeight = 28;

    public static void Attach(
        Form form,
        string title,
        ContextAction? action1 = null,
        ContextAction? action2 = null,
        ContextAction? action3 = null,
        ContextAction? action4 = null,
        ContextAction? action5 = null,
        ContextAction? action6 = null,
        ContextAction? action7 = null,
        ContextAction? action8 = null,
        ContextAction? action9 = null,
        ContextAction? action10 = null,
        ContextAction? action11 = null)
    {
        if (form.Controls.ContainsKey("__glowvaContextLayout"))
            return;

        form.RightToLeft = RightToLeft.No;
        form.RightToLeftLayout = false;
        form.KeyPreview = true;
        form.AutoScroll = false;
        form.HorizontalScroll.Enabled = false;
        form.VerticalScroll.Enabled = false;
        form.AutoScaleMode = AutoScaleMode.Font;
        form.BackColor = Color.White;

        var actions = new[]
        {
            action1, action2, action3, action4, action5, action6,
            action7, action8, action9, action10, action11
        };

        var existing = form.Controls.Cast<Control>().ToArray();
        form.Controls.Clear();

        var root = new TableLayoutPanel
        {
            Name = "__glowvaContextLayout",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 3,
            RowCount = 3,
            RightToLeft = RightToLeft.No,
            BackColor = Color.White
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LeftRailWidth));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RightRailWidth));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, MenuHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, StatusHeight));

        var menu = BuildTopMenu();
        var workArea = BuildWorkArea(existing);
        var leftRail = BuildLeftRail(form, title, actions.Where(a => a is not null).Select(a => a!).ToArray());
        var rightRail = BuildRightRail(form, title);
        var status = BuildStatusBar();

        root.Controls.Add(menu, 0, 0);
        root.SetColumnSpan(menu, 3);
        root.Controls.Add(leftRail, 0, 1);
        root.Controls.Add(workArea, 1, 1);
        root.Controls.Add(rightRail, 2, 1);
        root.Controls.Add(status, 0, 2);
        root.SetColumnSpan(status, 3);

        form.Controls.Add(root);
        form.MainMenuStrip = menu;
        root.BringToFront();
    }

    private static MenuStrip BuildTopMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Fill,
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = Color.FromArgb(255, 204, 74),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(8, 2, 8, 2),
            RightToLeft = RightToLeft.Yes,
            Renderer = new FlatMenuRenderer()
        };

        string[] entries =
        {
            "البيانات العامة", "العامة", "الأصناف", "المخازن", "الموردين", "المشتريات",
            "العملاء", "المبيعات", "الحسابات اليومية", "الإيصالات", "الأطباء", "شئون العاملين", "إطار"
        };

        foreach (var entry in entries)
            menu.Items.Add(CreateTopMenu(entry));

        return menu;
    }

    private static ToolStripMenuItem CreateTopMenu(string text)
    {
        var item = new ToolStripMenuItem(text)
        {
            Padding = new Padding(8, 3, 8, 3),
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleCenter
        };
        item.DropDownItems.Add(new ToolStripMenuItem("فتح") { RightToLeft = RightToLeft.Yes });
        return item;
    }

    private static Panel BuildWorkArea(Control[] existing)
    {
        var workArea = new Panel
        {
            Name = "__glowvaWorkArea",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.Yes
        };

        if (existing.Length > 0)
            workArea.Controls.AddRange(existing);

        return workArea;
    }

    private static Panel BuildLeftRail(Form owner, string title, ContextAction[] actions)
    {
        var panel = new Panel
        {
            Name = "__glowvaLeftRail",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(255, 204, 74),
            Padding = new Padding(4),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes
        };

        var host = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(255, 204, 74)
        };

        var visible = actions.Where(a => a.Text != "كارت الصنف").ToList();

        if (title == "المبيعات")
        {
            visible.Insert(0, new ContextAction(
                "استدعاء الفاتورة",
                form =>
                {
                    using var dialog = new InvoiceSearchForm(true);
                    dialog.ShowDialog(form);
                },
                Color.FromArgb(33, 150, 243)));
        }
        else if (title == "المشتريات")
        {
            visible.Insert(0, new ContextAction(
                "استدعاء الفاتورة",
                form =>
                {
                    using var dialog = new InvoiceSearchForm(false);
                    dialog.ShowDialog(form);
                },
                Color.FromArgb(33, 150, 243)));
        }

        foreach (var action in visible)
        {
            var button = new Button
            {
                Text = $"{GetIcon(action.Text)}\r\n{action.Text}",
                Width = LeftRailWidth - 8,
                Height = 66,
                Margin = new Padding(0, 1, 0, 3),
                Padding = Padding.Empty,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                BackColor = action.BackColor ?? Color.FromArgb(110, 110, 110),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                UseVisualStyleBackColor = false,
                TabStop = true
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(button.BackColor, 0.08F);
            button.Click += (_, _) => action.Execute(owner);
            host.Controls.Add(button);
        }

        panel.Controls.Add(host);
        return panel;
    }

    private static Panel BuildRightRail(Form owner, string activeTitle)
    {
        var panel = new Panel
        {
            Name = "__glowvaRightRail",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(255, 204, 74),
            Padding = new Padding(4),
            Margin = Padding.Empty,
            RightToLeft = RightToLeft.Yes
        };

        var buttons = new (string Text, string Icon, Color Color, Action Action)[]
        {
            ("النواقص", "⚠", Color.FromArgb(96, 125, 139), () => ShowInfo(owner, "الأصناف الناقصة")),
            ("الأصناف", "💊", Color.FromArgb(33, 150, 243), () => OpenDialog<ProductsForm>(owner)),
            ("المخازن", "🏬", Color.FromArgb(25, 118, 210), () => OpenDialog<InventoryForm>(owner)),
            ("المشتريات", "🛒", Color.FromArgb(123, 31, 162), () => OpenDialog<PurchasesForm>(owner)),
            ("المبيعات", "🛒", Color.FromArgb(30, 136, 229), () => OpenDialog<SalesForm>(owner)),
            ("تقرير المبيعات", "📊", Color.FromArgb(0, 125, 0), () => OpenDialog<SalesReportForm>(owner)),
            ("العملاء", "👥", Color.FromArgb(41, 128, 185), () => OpenDialog(() => new PartyListForm(false), owner))
        };

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = buttons.Length,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.Yes,
            BackColor = Color.FromArgb(255, 204, 74)
        };

        for (var i = 0; i < buttons.Length; i++)
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / buttons.Length));

        for (var i = 0; i < buttons.Length; i++)
        {
            var item = buttons[i];
            var active = string.Equals(activeTitle, item.Text, StringComparison.OrdinalIgnoreCase);
            var button = new Button
            {
                Text = $"{item.Icon}\r\n{item.Text}",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                BackColor = active ? Color.FromArgb(0, 102, 170) : item.Color,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                RightToLeft = RightToLeft.Yes,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = active ? 2 : 0;
            button.FlatAppearance.BorderColor = Color.White;
            button.Click += (_, _) => item.Action();
            host.Controls.Add(button, 0, i);
        }

        panel.Controls.Add(host);
        return panel;
    }

    private static StatusStrip BuildStatusBar()
    {
        var status = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false,
            BackColor = Color.FromArgb(255, 204, 74),
            Font = new Font("Segoe UI", 8.5F)
        };
        status.Items.Add(new ToolStripStatusLabel("المستخدم: Administrator") { Spring = true, TextAlign = ContentAlignment.MiddleRight });
        status.Items.Add(new ToolStripStatusLabel("جاهز") { Spring = true, TextAlign = ContentAlignment.MiddleCenter });
        status.Items.Add(new ToolStripStatusLabel("Glowva ERP") { Spring = true, TextAlign = ContentAlignment.MiddleLeft });
        return status;
    }

    private static void OpenDialog<T>(Form owner) where T : Form, new()
    {
        using var dialog = new T();
        Attach(dialog, dialog.Text);
        dialog.ShowDialog(owner);
    }

    private static void OpenDialog<T>(Func<T> factory, Form owner) where T : Form
    {
        using var dialog = factory();
        Attach(dialog, dialog.Text);
        dialog.ShowDialog(owner);
    }

    private static void ShowInfo(Form owner, string text)
    {
        MessageBox.Show(owner, text, "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string GetIcon(string text) => text switch
    {
        "جديد" => "✚",
        "حفظ" => "💾",
        "استدعاء الفاتورة" => "🔎",
        "بحث الأصناف" => "🔎",
        "إضافة صنف" => "💊",
        "سطر جديد" => "＋",
        "حذف سطر" => "✖",
        "فواتير مرتجعة" => "↩",
        "مرتجعات المبيعات" => "↩",
        "مرتجعات المشتريات" => "↩",
        "فواتير غير مكتملة" => "📄",
        "تعليق الفاتورة" => "⏸",
        "العملاء" => "👥",
        "الموردين" => "🏢",
        "المبيعات" => "🛒",
        "المشتريات" => "📦",
        "الأصناف" => "💊",
        "المخزون" => "📊",
        "الحسابات" => "🧾",
        "الخزينة" => "💰",
        "طباعة المستخدم" => "🖨",
        "لوحة التحكم" => "⌂",
        "إغلاق" => "✖",
        _ => "•"
    };

    private sealed class FlatMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb(255, 204, 74));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }
    }
}

public sealed record ContextAction(string Text, Action<Form> Execute, Color? BackColor = null);
