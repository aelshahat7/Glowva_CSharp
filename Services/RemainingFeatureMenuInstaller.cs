using GlowvaERP.Forms;

namespace GlowvaERP.Services;

public static class RemainingFeatureMenuInstaller
{
    public static void Install(Form shell)
    {
        var menu=shell.MainMenuStrip;
        if(menu==null)return;
        var root=new ToolStripMenuItem("الأدوات المتقدمة"){RightToLeft=RightToLeft.Yes};
        Add(root,"تسوية وجرد المخزون",()=>Show(shell,new InventoryAdjustmentForm()));
        Add(root,"المستخدمون والصلاحيات",()=>Show(shell,new UserManagementForm()));
        Add(root,"مركز التقارير",()=>Show(shell,new ReportCenterForm()));
        menu.Items.Add(root);
    }
    private static void Add(ToolStripMenuItem parent,string text,Action action){var x=new ToolStripMenuItem(text){RightToLeft=RightToLeft.Yes};x.Click+=(_,_)=>action();parent.DropDownItems.Add(x);}
    private static void Show(Form owner,Form form){using(form)form.ShowDialog(owner);}
}
