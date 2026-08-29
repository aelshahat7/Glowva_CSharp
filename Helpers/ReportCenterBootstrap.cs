using System.Runtime.CompilerServices;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

internal static class ReportCenterBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Application.Idle += InstallWhenMainFormReady;
    }

    private static void InstallWhenMainFormReady(object? sender, EventArgs e)
    {
        var main = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.GetType().Name == "MainForm");
        if (main?.MainMenuStrip is not MenuStrip menu)
            return;

        var accounts = Find(menu.Items, "الحسابات");
        if (accounts is null)
            return;

        var reports = Find(accounts.DropDownItems, "تقارير الحسابات");
        if (reports is null)
            return;

        var oldCustomerReport = Find(reports.DropDownItems, "كشف حساب العملاء");
        if (oldCustomerReport is not null)
            reports.DropDownItems.Remove(oldCustomerReport);

        if (Find(reports.DropDownItems, "كشف حساب العملاء") is null)
        {
            var item = new ToolStripMenuItem("كشف حساب العملاء")
            {
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(8, 5, 8, 5)
            };
            item.Click += (_, _) =>
            {
                using var report = new CustomerAccountsReportForm();
                ContextualSidebar.Attach(report, "كشف حساب العملاء",
                    new ContextAction("تشغيل", f => { }, Color.FromArgb(33, 150, 243)),
                    new ContextAction("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
                report.ShowDialog(main);
            };
            reports.DropDownItems.Add(item);
        }

        if (Find(menu.Items, "مركز التقارير") is null)
        {
            var center = new ToolStripMenuItem("مركز التقارير")
            {
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(10, 4, 10, 4)
            };
            var customer = new ToolStripMenuItem("كشف حساب العملاء")
            {
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(8, 5, 8, 5)
            };
            customer.Click += (_, _) =>
            {
                using var report = new CustomerAccountsReportForm();
                ContextualSidebar.Attach(report, "كشف حساب العملاء",
                    new ContextAction("تشغيل", f => { }, Color.FromArgb(33, 150, 243)),
                    new ContextAction("إغلاق", f => f.Close(), Color.FromArgb(80, 80, 80)));
                report.ShowDialog(main);
            };
            center.DropDownItems.Add(customer);
            menu.Items.Add(center);
        }

        Application.Idle -= InstallWhenMainFormReady;
    }

    private static ToolStripMenuItem? Find(ToolStripItemCollection items, string text)
    {
        foreach (ToolStripItem item in items)
            if (item is ToolStripMenuItem menuItem && string.Equals(menuItem.Text, text, StringComparison.Ordinal))
                return menuItem;
        return null;
    }
}
