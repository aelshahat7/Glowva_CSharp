using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

internal static class ReferenceWindowMenuBridge
{
    private static readonly IReadOnlyDictionary<string, Func<Form>> Windows = new Dictionary<string, Func<Form>>(StringComparer.Ordinal)
    {
        ["بيانات المؤسسة"] = () => new OrganizationDataForm(),
        ["إعدادات التشغيل"] = () => new OperatingSettingsForm(),
        ["إعدادات طباعة فاتورة البيع"] = () => new SalesInvoicePrintSettingsForm(),
        ["إعدادات طباعة الباركود"] = () => new BarcodePrintSettingsForm(),
        ["أخذ نسخة احتياطية"] = () => new BackupForm(),
        ["نسخ احتياطية دورية"] = () => new ScheduledBackupForm(),
        ["حجم قاعدة البيانات"] = () => new DatabaseSizeForm(),
        ["طباعة باركود"] = () => new BarcodePrintForm(),
        ["إصدار فاتورة ورقية للتعاقد"] = () => new ContractInvoiceForm()
    };

    public static void Install(WorkspaceShellForm shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null)
            return;

        Wire(menu.Items, shell);
    }

    private static void Wire(ToolStripItemCollection items, WorkspaceShellForm shell)
    {
        for (var index = items.Count - 1; index >= 0; index--)
        {
            if (items[index] is not ToolStripMenuItem item)
                continue;

            if (item.DropDownItems.Count > 0)
            {
                Wire(item.DropDownItems, shell);
                continue;
            }

            if (!Windows.TryGetValue(item.Text, out var factory))
                continue;

            var replacement = new ToolStripMenuItem(item.Text)
            {
                RightToLeft = item.RightToLeft,
                Font = item.Font,
                TextAlign = item.TextAlign,
                Enabled = item.Enabled,
                Checked = item.Checked,
                CheckOnClick = item.CheckOnClick,
                Image = item.Image,
                ImageScaling = item.ImageScaling,
                DisplayStyle = item.DisplayStyle,
                BackColor = item.BackColor,
                ForeColor = item.ForeColor
            };

            replacement.Click += (_, _) =>
            {
                var form = factory();
                form.Show(shell);
                form.Activate();
            };

            items.RemoveAt(index);
            items.Insert(index, replacement);
        }
    }
}
