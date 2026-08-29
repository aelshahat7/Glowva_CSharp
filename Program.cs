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
        UiRtl.InstallGlobal(); UiTheme.InstallGlobal(); ContextualSearchShortcuts.InstallGlobal();
        Database.Initialize(); SchemaUpgradeService.Apply();
        using(var login=new LoginForm()) if(login.ShowDialog()!=DialogResult.OK) return;
        var shell=new WorkspaceShellForm();
        WorkspaceFeatureBootstrap.Install(shell); RemainingFeatureMenuInstaller.Install(shell);
        InstallNewMenus(shell);
        Application.Run(shell);
    }
    static void InstallNewMenus(Form shell)
    {
        var menu=shell.MainMenuStrip; if(menu==null)return;
        var tools=new ToolStripMenuItem("إدارة النظام"){RightToLeft=RightToLeft.Yes};
        Add(tools,"المخازن والتحويلات",()=>Show(shell,new WarehousesForm()));
        Add(tools,"الأرباح والتكلفة",()=>Show(shell,new ProfitReportForm()));
        Add(tools,"استيراد البيانات القديمة",()=>Show(shell,new LegacyImportForm()));
        Add(tools,"المستخدمون والصلاحيات",()=>Show(shell,new UserManagementForm()));
        Add(tools,"الإعدادات والنسخ الاحتياطي",()=>Show(shell,new SettingsForm()));
        menu.Items.Add(tools);
    }
    static void Add(ToolStripMenuItem p,string text,Action a){var x=new ToolStripMenuItem(text){RightToLeft=RightToLeft.Yes};x.Click+=(_,_)=>a();p.DropDownItems.Add(x);}
    static void Show(Form owner,Form f){using(f)f.ShowDialog(owner);}
}
