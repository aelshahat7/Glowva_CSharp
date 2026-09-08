using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

internal static class ReferenceWindowMenuBridge
{
    private static readonly IReadOnlyDictionary<string, string> WindowTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["بيانات المؤسسة"] = "OrganizationDataForm",
        ["إعدادات التشغيل"] = "OperatingSettingsForm",
        ["إعدادات طباعة فاتورة البيع"] = "SalesInvoicePrintSettingsForm",
        ["إعدادات طباعة الباركود"] = "BarcodePrintSettingsForm",
        ["أخذ نسخة احتياطية"] = "BackupForm",
        ["نسخ احتياطية دورية"] = "ScheduledBackupForm",
        ["حجم قاعدة البيانات"] = "DatabaseSizeForm",
        ["طباعة باركود"] = "BarcodePrintForm",
        ["إصدار فاتورة ورقية للتعاقد"] = "ContractInvoiceForm"
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

            if (!WindowTypes.TryGetValue(item.Text, out var typeName))
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

            replacement.Click += (_, _) => OpenReferenceWindow(shell, typeName, item.Text);

            items.RemoveAt(index);
            items.Insert(index, replacement);
        }
    }

    private static void OpenReferenceWindow(WorkspaceShellForm shell, string typeName, string title)
    {
        var type = typeof(ReferenceWindowMenuBridge).Assembly.GetType($"GlowvaERP.Forms.{typeName}");
        if (type == null || !typeof(Form).IsAssignableFrom(type))
        {
            MessageBox.Show(shell, $"النافذة المرجعية \"{title}\" غير متاحة حاليًا.", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var form = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, null) as Form;
            if (form == null)
            {
                MessageBox.Show(shell, $"تعذر إنشاء النافذة \"{title}\".", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            form.Show(shell);
            form.Activate();
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            MessageBox.Show(shell, $"تعذر فتح \"{title}\":\n{ex.InnerException.Message}", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(shell, $"تعذر فتح \"{title}\":\n{ex.Message}", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
