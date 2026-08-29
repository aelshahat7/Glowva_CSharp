using System.Drawing;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

public static class ReportMenuInstaller
{
    public static void Install(Form mainForm)
    {
        if (mainForm.MainMenuStrip is not MenuStrip menu) return;
        var reportsMenu = FindItem(menu.Items, "التقارير");
        if (reportsMenu is null) return;
        if (FindItem(reportsMenu.DropDownItems, "تقارير المبيعات") is not null) return;
        AddReport(mainForm,reportsMenu,"تقارير المبيعات","SELECT o.invoice_number AS [رقم الفاتورة], o.order_date AS [التاريخ], COALESCE(c.name,'عميل نقدي') AS [العميل], o.payment_status AS [طريقة الدفع], COALESCE(SUM(oi.quantity*oi.unit_price),0)-COALESCE(o.discount,0) AS [الإجمالي] FROM orders o LEFT JOIN customers c ON c.id=o.customer_id LEFT JOIN order_items oi ON oi.order_id=o.id WHERE date(o.order_date) BETWEEN date($from) AND date($to) GROUP BY o.id ORDER BY o.order_date DESC, o.id DESC;");
        AddReport(mainForm,reportsMenu,"تقارير المشتريات","SELECT p.invoice_number AS [رقم الفاتورة], p.purchase_date AS [التاريخ], COALESCE(s.name,'غير محدد') AS [المورد], p.payment_status AS [طريقة الدفع], COALESCE(SUM(pi.quantity*pi.unit_price),0)-COALESCE(p.discount,0) AS [الإجمالي] FROM purchases p LEFT JOIN suppliers s ON s.id=p.supplier_id LEFT JOIN purchase_items pi ON pi.purchase_id=p.id WHERE date(p.purchase_date) BETWEEN date($from) AND date($to) GROUP BY p.id ORDER BY p.purchase_date DESC, p.id DESC;");
        AddReport(mainForm,reportsMenu,"تقارير حركة المخزون","SELECT il.movement_date AS [التاريخ], p.code AS [الكود], p.name AS [الصنف], il.movement_type AS [الحركة], il.quantity_in AS [وارد], il.quantity_out AS [منصرف], il.unit_cost AS [التكلفة] FROM inventory_ledger il JOIN products p ON p.id=il.product_id WHERE date(il.movement_date) BETWEEN date($from) AND date($to) ORDER BY il.movement_date DESC, il.id DESC;");
        AddReport(mainForm,reportsMenu,"تقارير الخزينة","SELECT transaction_date AS [التاريخ], transaction_type AS [الحركة], amount_in AS [وارد], amount_out AS [منصرف], notes AS [ملاحظات] FROM cash_transactions WHERE date(transaction_date) BETWEEN date($from) AND date($to) ORDER BY transaction_date DESC, id DESC;");
        AddReport(mainForm,reportsMenu,"تقارير الأرباح","SELECT date(o.order_date) AS [التاريخ], COALESCE(SUM(oi.quantity*oi.unit_price),0)-COALESCE(o.discount,0) AS [المبيعات], COALESCE(SUM(oi.quantity*oi.cost_price),0) AS [التكلفة], (COALESCE(SUM(oi.quantity*oi.unit_price),0)-COALESCE(o.discount,0)-COALESCE(SUM(oi.quantity*oi.cost_price),0)) AS [الربح] FROM orders o LEFT JOIN order_items oi ON oi.order_id=o.id WHERE date(o.order_date) BETWEEN date($from) AND date($to) GROUP BY date(o.order_date) ORDER BY date(o.order_date) DESC;");
    }
    private static void AddReport(Form owner,ToolStripMenuItem parent,string title,string sql){var item=new ToolStripMenuItem(title){RightToLeft=RightToLeft.Yes,TextAlign=ContentAlignment.MiddleRight,Padding=new Padding(8,5,8,5)};item.Click+=(_,_)=>{using var report=new ReportViewerForm(title,sql);ContextualSidebar.Attach(report,title,new ContextAction("بحث",_=>report.Activate(),Color.FromArgb(33,150,243)),new ContextAction("إغلاق",_=>report.Close(),Color.FromArgb(80,80,80)));report.ShowDialog(owner);};parent.DropDownItems.Add(item);}
    private static ToolStripMenuItem? FindItem(ToolStripItemCollection items,string text){foreach(ToolStripItem item in items)if(item is ToolStripMenuItem menuItem&&string.Equals(menuItem.Text,text,StringComparison.Ordinal))return menuItem;return null;}
}
