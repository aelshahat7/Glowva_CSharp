using GlowvaERP.Forms;

namespace GlowvaERP.Services;

public static class WorkspaceFeatureBootstrap
{
    public static void Install(Form shell)
    {
        var menu=shell.MainMenuStrip;
        if(menu==null)return;
        var root=new ToolStripMenuItem("التقارير والإدارة"){RightToLeft=RightToLeft.Yes};
        Add(root,"مركز التقارير",()=>Show(shell,new ReportCenterForm()));
        Add(root,"تقرير المبيعات",()=>Show(shell,new ReportCenterForm("تقرير المبيعات")));
        Add(root,"تقرير المشتريات",()=>Show(shell,new ReportCenterForm("تقرير المشتريات")));
        Add(root,"كشف حساب العملاء",()=>Show(shell,new ReportCenterForm("كشف حساب العملاء")));
        Add(root,"كشف حساب الموردين",()=>Show(shell,new ReportCenterForm("كشف حساب الموردين")));
        Add(root,"تقرير الخزينة",()=>Show(shell,new ReportCenterForm("تقرير الخزينة")));
        Add(root,"تقرير المخزون",()=>Show(shell,new ReportCenterForm("تقرير المخزون")));
        Add(root,"الأصناف الناقصة",()=>Show(shell,new ReportCenterForm("الأصناف الناقصة")));
        Add(root,"حركة المخزون",()=>Show(shell,new ReportCenterForm("حركة المخزون")));
        Add(root,"تقرير المصروفات",()=>Show(shell,new ReportCenterForm("تقرير المصروفات")));
        root.DropDownItems.Add(new ToolStripSeparator());
        Add(root,"إدارة مرتجعات المبيعات",()=>Show(shell,new ReturnsManagerForm(true)));
        Add(root,"إدارة مرتجعات المشتريات",()=>Show(shell,new ReturnsManagerForm(false)));
        Add(root,"المصروفات",()=>Show(shell,new ExpensesForm()));
        Add(root,"سحب الأرباح",()=>Show(shell,new ProfitPayoutForm()));
        Add(root,"الإعدادات والنسخ الاحتياطي",()=>Show(shell,new SettingsForm()));
        menu.Items.Add(root);
    }
    private static void Add(ToolStripMenuItem parent,string text,Action action){var x=new ToolStripMenuItem(text){RightToLeft=RightToLeft.Yes};x.Click+=(_,_)=>action();parent.DropDownItems.Add(x);}
    private static void Show(Form owner,Form form){using(form)form.ShowDialog(owner);}
}
