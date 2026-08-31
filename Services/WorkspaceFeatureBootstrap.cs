using System;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Services;

public static class WorkspaceFeatureBootstrap
{
    public static void Install(Form shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null) return;

        var root = new ToolStripMenuItem("التقارير والإدارة") { RightToLeft = RightToLeft.Yes };
        Add(root, "مركز التقارير", shell, () => new ReportCenterForm());
        Add(root, "تقرير المبيعات", shell, () => new ReportCenterForm("تقرير المبيعات"));
        Add(root, "تقرير المشتريات", shell, () => new ReportCenterForm("تقرير المشتريات"));
        Add(root, "كشف حساب العملاء", shell, () => new ReportCenterForm("كشف حساب العملاء"));
        Add(root, "كشف حساب الموردين", shell, () => new ReportCenterForm("كشف حساب الموردين"));
        Add(root, "تقرير الخزينة", shell, () => new ReportCenterForm("تقرير الخزينة"));
        Add(root, "تقرير المخزون", shell, () => new ReportCenterForm("تقرير المخزون"));
        Add(root, "الأصناف الناقصة", shell, () => new ReportCenterForm("الأصناف الناقصة"));
        Add(root, "حركة المخزون", shell, () => new ReportCenterForm("حركة المخزون"));
        Add(root, "تقرير المصروفات", shell, () => new ReportCenterForm("تقرير المصروفات"));
        root.DropDownItems.Add(new ToolStripSeparator());
        Add(root, "إدارة مرتجعات المبيعات", shell, () => new ReturnsManagerForm(true));
        Add(root, "إدارة مرتجعات المشتريات", shell, () => new ReturnsManagerForm(false));
        Add(root, "المصروفات", shell, () => new ExpensesForm());
        Add(root, "سحب الأرباح", shell, () => new ProfitPayoutForm());
        Add(root, "الإعدادات والنسخ الاحتياطي", shell, () => new SettingsForm());
        menu.Items.Add(root);
    }

    private static void Add(ToolStripMenuItem parent, string text, Form shell, Func<Form> factory)
    {
        var x = new ToolStripMenuItem(text) { RightToLeft = RightToLeft.Yes };
        x.Click += (_, _) => Open(shell, factory, text);
        parent.DropDownItems.Add(x);
    }

    private static void Open(Form shell, Func<Form> factory, string title)
    {
        if (shell is WorkspaceShellForm workspace)
        {
            workspace.OpenChild(factory, title);
            return;
        }

        using var form = factory();
        form.ShowDialog(shell);
    }
}
