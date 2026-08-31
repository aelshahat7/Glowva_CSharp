using System;
using System.Windows.Forms;
using GlowvaERP.Data;
using GlowvaERP.Helpers;
using GlowvaERP.Services;
using GlowvaERP.Forms;

namespace GlowvaERP;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        UiRtl.InstallGlobal();
        UiTheme.InstallGlobal();
        ContextualSearchShortcuts.InstallGlobal();
        Database.Initialize();
        SchemaUpgradeService.Apply();

        SalesDirectCodeEntry.InstallGlobal();

        using (var login = new LoginForm())
        {
            if (login.ShowDialog() != DialogResult.OK)
                return;
        }

        var shell = new WorkspaceShellForm();
        WorkspaceFeatureBootstrap.Install(shell);
        RemainingFeatureMenuInstaller.Install(shell);
        InstallNewMenus(shell);
        Application.Run(shell);
    }

    private static void InstallNewMenus(WorkspaceShellForm shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null) return;

        var tools = new ToolStripMenuItem("إدارة النظام")
        {
            RightToLeft = RightToLeft.Yes
        };

        Add(tools, "المخازن والتحويلات", shell, () => new WarehousesForm());
        Add(tools, "الأرباح والتكلفة", shell, () => new ProfitReportForm());
        Add(tools, "استيراد البيانات القديمة", shell, () => new LegacyImportForm());
        Add(tools, "المستخدمون والصلاحيات", shell, () => new UserManagementForm());
        Add(tools, "الإعدادات والنسخ الاحتياطي", shell, () => new SettingsForm());
        menu.Items.Add(tools);
    }

    private static void Add(ToolStripMenuItem parent, string text, WorkspaceShellForm shell, Func<Form> factory)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes
        };
        item.Click += (_, _) => shell.OpenChild(factory, text);
        parent.DropDownItems.Add(item);
    }
}
