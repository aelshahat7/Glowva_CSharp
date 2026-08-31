using System;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Services;

public static class RemainingFeatureMenuInstaller
{
    public static void Install(Form shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null) return;

        var root = new ToolStripMenuItem("الأدوات المتقدمة") { RightToLeft = RightToLeft.Yes };
        Add(root, "تسوية وجرد المخزون", shell, () => new InventoryAdjustmentForm());
        Add(root, "المستخدمون والصلاحيات", shell, () => new UserManagementForm());
        Add(root, "مركز التقارير", shell, () => new ReportCenterForm());
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
