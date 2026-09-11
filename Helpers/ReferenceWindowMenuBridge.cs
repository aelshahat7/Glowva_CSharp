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
        ["بيانات المؤسسة"] = "OrganizationDataV2Form",
        ["إعدادات التشغيل"] = "OperatingSettingsV2Form",
        ["إعدادات طباعة فاتورة البيع"] = "SalesInvoicePrintSettingsV2Form",
        ["إعدادات طباعة الباركود"] = "BarcodePrintSettingsV2Form",
        ["أخذ نسخة احتياطية"] = "BackupV2Form",
        ["نسخ احتياطية دورية"] = "ScheduledBackupV2Form",
        ["حجم قاعدة البيانات"] = "DatabaseSizeV2Form",
        ["طباعة باركود"] = "BarcodePrintV2Form",
        ["إصدار فاتورة ورقية للتعاقد"] = "ContractInvoiceV2Form"
    };

    public static void Install(WorkspaceShellForm shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null) return;
        Wire(menu.Items, shell);
    }

    private static void Wire(ToolStripItemCollection items, WorkspaceShellForm shell)
    {
        for (var index = items.Count - 1; index >= 0; index--)
        {
            if (items[index] is not ToolStripMenuItem item) continue;
            if (item.DropDownItems.Count > 0)
            {
                Wire(item.DropDownItems, shell);
                continue;
            }

            var menuText = item.Text ?? string.Empty;
            if (!WindowTypes.TryGetValue(menuText, out var typeName)) continue;

            var replacement = new ToolStripMenuItem(menuText)
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
            replacement.Click += (_, _) => OpenReferenceWindow(shell, typeName, menuText);
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

            ReferenceWindowRuntimeFix.Apply(form);
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowDialog(shell);
        }
        catch (TargetInvocationException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            MessageBox.Show(shell, $"تعذر فتح النافذة \"{title}\":\n{message}", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(shell, $"تعذر فتح النافذة \"{title}\":\n{ex.Message}", "Glowva ERP", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
