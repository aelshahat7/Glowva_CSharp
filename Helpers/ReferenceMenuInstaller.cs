using System;
using System.Drawing;
using System.Windows.Forms;
using GlowvaERP.Forms;

namespace GlowvaERP.Helpers;

/// <summary>
/// Rebuilds the visible menu system from the supplied legacy ERP screenshots.
/// Menu text and hierarchy are reference-driven; unimplemented actions are safe placeholders.
/// </summary>
public static class ReferenceMenuInstaller
{
    public static void Install(WorkspaceShellForm shell)
    {
        var menu = shell.MainMenuStrip;
        if (menu == null)
            return;

        // Keep the existing WindowMenu object because WorkspaceShellForm owns and updates it.
        var windowMenu = menu.Items.Find("WindowMenu", true).Length > 0
            ? menu.Items.Find("WindowMenu", true)[0] as ToolStripMenuItem
            : null;

        menu.SuspendLayout();
        menu.Items.Clear();

        menu.RightToLeft = RightToLeft.Yes;
        menu.Font = new Font("Tahoma", 9.5F, FontStyle.Bold);
        menu.Padding = new Padding(3, 0, 3, 0);
        menu.Height = 31;
        menu.BackColor = Color.FromArgb(255, 204, 74);
        menu.ForeColor = Color.Black;
        menu.GripStyle = ToolStripGripStyle.Hidden;

        menu.Items.Add(Menu("البيانات العامة",
            Item("بيانات المؤسسة", shell),
            Sep(),
            Item("إعدادات التشغيل", shell),
            Item("إعدادات طباعة فاتورة البيع", shell),
            Item("إعدادات طباعة الباركود", shell),
            Sep(),
            Item("أخذ نسخة احتياطية", shell),
            Item("نسخ احتياطية دورية", shell),
            Item("حجم قاعدة البيانات", shell),
            Sep(),
            Item("طباعة باركود", shell),
            Item("فتح الدرج", shell),
            Sep(),
            Item("إصدار فاتورة ورقية للتعاقد", shell),
            Item("Update System", shell)));

        menu.Items.Add(Menu("الأصناف",
            Item("قائمة الأصناف", shell, () => shell.OpenChild(() => new ProductsForm(), "قائمة الأصناف")),
            Item("وحدات الأصناف", shell),
            Item("الجرعات الدوائية", shell),
            Sep(),
            Item("الشركات المنتجة", shell),
            Item("تقرير أصناف بالشركة المنتجة", shell),
            Sep(),
            Item("أماكن الأصناف", shell),
            Item("تحديد أماكن الأصناف", shell),
            Item("تقرير أصناف حسب مكان الصنف", shell),
            Sep(),
            Item("مجموعات الأصناف", shell),
            Item("تحديد المجموعة العلمية للأصناف", shell),
            Item("تقرير أصناف حسب المجموعة العلمية", shell),
            Sep(),
            Item("الشكل الصيدلي", shell),
            Item("تحديد الشكل الصيدلي للأصناف", shell),
            Item("تقرير أصناف حسب الشكل الصيدلي", shell),
            Sep(),
            Item("تقرير إضافة الأصناف", shell),
            Item("تقرير أصناف تغير أسعارها", shell),
            Item("تقرير أصناف تغير معاملات وحداتها", shell),
            Sep(),
            Item("تعديل أسعار بيع الأصناف", shell)));

        menu.Items.Add(Menu("المخازن",
            Item("المخازن الداخلية للفرع", shell, () => shell.OpenChild(() => new WarehousesForm(), "المخازن الداخلية للفرع")),
            Item("تحويل أصناف بين المخازن", shell),
            Item("تقرير تحويل الأصناف بين المخازن", shell),
            Sep(),
            Item("تعديل تكلفة الأصناف الموجودة بالمخزن", shell),
            Sep(),
            Item("جرد و ضبط كميات الأصناف", shell),
            Item("تقرير بتعديلات كميات أصناف", shell),
            Sep(),
            Item("تقرير كميات أصناف المخازن طبقاً لتواريخ الصلاحية", shell),
            Item("تقرير كميات أصناف مخازن", shell),
            Item("تقرير طباعة الجرد للمخازن", shell),
            Sep(),
            Item("تقرير أصناف منتهية الصلاحية في المخزن", shell),
            Item("تقرير حركة صنف في المخزن", shell),
            Sep(),
            Item("الأرصدة الافتتاحية للمخزن", shell),
            Item("تقرير الأرصدة الافتتاحية للأصناف", shell),
            Sep(),
            Item("الجرد الدوري", shell),
            Item("تقرير الجرد الدوري", shell)));

        menu.Items.Add(Menu("الموردين",
            Item("قائمة الموردين", shell, () => shell.OpenChild(() => new PartyListForm(true), "قائمة الموردين")),
            Item("تقرير عن الموردين", shell),
            Item("تعديل أسعار مورد", shell),
            Item("تقرير أصناف مورد", shell),
            Sep(),
            Item("مقارنة أسعار صنف للموردين", shell),
            Item("الأرصدة الافتتاحية للموردين", shell),
            Item("كشف حساب مورد", shell)));

        menu.Items.Add(Menu("المشتريات",
            Item("فاتورة شراء", shell, () => shell.OpenChild(() => new PurchasesForm(), "فاتورة شراء")),
            Item("مرتجع شراء من فاتورة", shell),
            Sep(),
            Item("مرتجع شراء بدون فاتورة", shell),
            Sep(),
            Item("تقرير ملخص فواتير المشتريات", shell),
            Item("تقرير فواتير المشتريات بالأصناف", shell),
            Sep(),
            Item("تقرير حركة مشتريات صنف", shell),
            Item("تقرير إجمالي المرتجعات لمورد", shell),
            Sep(),
            Item("تقرير إجمالي مشتريات و مرتجعات مورد", shell),
            Item("تقرير مقارنة قيمة المشتريات طبقاً لقيمة المبيعات شهرياً", shell),
            Sep(),
            Item("تقرير بونص مشتريات الأصناف", shell),
            Item("تقرير مشتريات الأصناف الضريبية", shell)));

        menu.Items.Add(Menu("العملاء",
            Item("قائمة العملاء", shell, () => shell.OpenChild(() => new PartyListForm(false), "قائمة العملاء")),
            Item("تقرير بالعملاء", shell),
            Sep(),
            Item("التعاقدات", shell),
            Item("مناطق العملاء", shell),
            Sep(),
            Item("الأرصدة الافتتاحية للعملاء", shell),
            Item("تقرير عن العملاء بالمنطقة", shell),
            Sep(),
            Item("كشف حساب عميل", shell),
            Item("تقرير مبيعات أصناف عميل", shell)));

        menu.Items.Add(Menu("المبيعات",
            Item("فاتورة المبيعات", shell, () => shell.OpenChild(() => new SalesForm(), "فاتورة المبيعات")),
            Item("مرتجع المبيعات من فاتورة", shell),
            Item("إقفال الفواتير المعلقة", shell),
            Item("استبدال أصناف", shell),
            Sep(),
            Item("تقرير فواتير المبيعات عن فترة", shell),
            Item("تقرير مبيعات أصناف عن فترة", shell),
            Item("تقرير مرتجع المبيعات عن فترة", shell),
            Item("تقرير حركة بيع الأصناف", shell),
            Item("تقرير كميات أصناف لم تباع", shell),
            Sep(),
            Item("تقرير مبيعات الموظفين يومى", shell),
            Item("تقرير مندوبين التوصيل المنزلي", shell),
            Sep(),
            Item("الكاشير", shell),
            Item("تقفيل درج الكاشير", shell),
            Item("تقرير تقفيل درج الكاشير", shell),
            Item("تقرير مبيعات الفيزا", shell),
            Sep(),
            Item("تقرير مبيعات بالشركة المنتجة للأصناف", shell),
            Item("تقرير مبيعات العملاء", shell),
            Sep(),
            Item("تقرير قيمة المبيعات باليوم", shell),
            Item("تقرير بقيم انواع المبيعات شهرى", shell),
            Item("تقرير تكلفة المبيعات ونسبة الربح", shell),
            Sep(),
            Item("تقرير فواتير البيع لصاحب التعاقد", shell),
            Item("تقرير إجمالى فواتير البيع لصاحب التعاقد", shell),
            Item("تقرير فواتير البيع بالأصناف لصاحب التعاقد", shell),
            Item("تقرير إجمالى بيع التعاقد", shell)));

        menu.Items.Add(Menu("الحسابات اليومية",
            Item("النقدية المتاحة", shell),
            Item("صرف نقدية", shell),
            Item("توريد نقدية", shell),
            Item("سحب نقدية من حساب البنك", shell),
            Sep(),
            Item("تقرير المصروفات النقدية", shell),
            Item("تقرير التوريدات النقدية", shell),
            Item("تقرير تحويلات النقدية", shell),
            Sep(),
            Item("إصدار شيك", shell),
            Item("استلام شيك", shell),
            Sep(),
            Item("تقفيل الشيكات المستلمة", shell),
            Item("تقفيل الشيكات الصادرة", shell),
            Sep(),
            Item("تقرير الشيكات المستلمة", shell),
            Item("تقرير الشيكات الصادرة", shell),
            Item("تقرير شيكات البنك طبقاً لتاريخ الاستحقاق", shell)));

        menu.Items.Add(Menu("الإيصالات"));
        menu.Items.Add(Menu("الأطباء",
            Item("ضبط حد الطلب للأصناف", shell),
            Item("أدوية بيطرية", shell),
            Item("شكل النواقص", shell),
            Item("تقرير أصناف وصلت حد الطلب", shell)));

        menu.Items.Add(Menu("شئون العاملين",
            Item("الوظائف", shell),
            Item("الموظفين", shell),
            Item("صلاحيات الموظفين", shell),
            Sep(),
            Item("الحضور و الإنصراف", shell),
            Item("تقرير الحضور و الإنصراف", shell),
            Sep(),
            Item("تسجيل الغياب والإجازات", shell),
            Item("تسجيل خصم الغياب للموظفين", shell),
            Item("تقرير خصم الغياب", shell),
            Sep(),
            Item("حساب عمولة مندوب البيع", shell),
            Item("تقرير عمولات البيع", shell),
            Sep(),
            Item("تسجيل خصم لموظف", shell),
            Item("تقرير الخصومات", shell),
            Sep(),
            Item("تسجيل حوافز و بدلات لموظف", shell),
            Item("تقرير الحوافز و البدلات", shell),
            Item("صرف سلف عاملين", shell),
            Item("توريد سلف عاملين", shell),
            Item("تقرير سلف العاملين", shell),
            Sep(),
            Item("تسجيل كشف المرتبات", shell),
            Item("صرف رواتب الموظفين", shell),
            Item("تقرير المرتبات", shell),
            Sep(),
            Item("تقرير تسجيل الدخول للبرنامج", shell)));

        if (windowMenu != null)
        {
            windowMenu.DropDownItems.Clear();
            windowMenu.Text = "إطار";
            windowMenu.Name = "WindowMenu";
            windowMenu.RightToLeft = RightToLeft.Yes;
            windowMenu.DropDownItems.Add(Item("عن البرنامج", shell, () => shell.Focus()));
            menu.Items.Add(windowMenu);
        }

        menu.Items.Add(Menu("التقارير والإدارة"));
        menu.Items.Add(Menu("الأدوات المتقدمة"));
        menu.Items.Add(Menu("إدارة النظام"));

        menu.ResumeLayout(true);
    }

    private static ToolStripMenuItem Menu(string text, params ToolStripItem[] children)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Tahoma", 9.5F, FontStyle.Bold)
        };
        if (children.Length > 0)
            item.DropDownItems.AddRange(children);
        else
            item.Click += (_, _) => { };
        return item;
    }

    private static ToolStripMenuItem Item(string text, WorkspaceShellForm shell, Action? action = null)
    {
        var item = new ToolStripMenuItem(text)
        {
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Tahoma", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight
        };
        item.Click += (_, _) =>
        {
            if (action != null)
            {
                action();
                return;
            }

            MessageBox.Show(
                shell,
                $"وظيفة \"{text}\" سيتم تنفيذها لاحقًا.",
                "Glowva ERP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };
        return item;
    }

    private static ToolStripSeparator Sep() => new ToolStripSeparator();
}
