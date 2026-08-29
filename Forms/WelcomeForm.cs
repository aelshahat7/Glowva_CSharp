using System.Drawing;
using System.Windows.Forms;

namespace GlowvaERP.Forms;

public sealed class WelcomeForm : Form
{
    public WelcomeForm()
    {
        Text = "مرحبًا";
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(50),
            BackColor = Color.White,
            RightToLeft = RightToLeft.Yes
        };

        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

        var brand = new Label
        {
            Dock = DockStyle.Fill,
            Text = "GLOWVA ERP",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 34F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 105, 180),
            RightToLeft = RightToLeft.No
        };

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "مرحبًا بك في نظام Glowva لإدارة الصيدلية",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 35, 35),
            RightToLeft = RightToLeft.Yes
        };

        var subtitle = new Label
        {
            Dock = DockStyle.Fill,
            Text = "مبيعات • مشتريات • مخزون • أصناف • عملاء • حسابات",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 13F),
            ForeColor = Color.FromArgb(100, 100, 100),
            RightToLeft = RightToLeft.Yes
        };

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "استخدم شريط القوائم بالأعلى أو الأزرار الجانبية للوصول إلى أقسام البرنامج.",
            TextAlign = ContentAlignment.TopCenter,
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(130, 130, 130),
            Padding = new Padding(10),
            RightToLeft = RightToLeft.Yes
        };

        root.Controls.Add(brand, 0, 0);
        root.Controls.Add(title, 0, 1);
        root.Controls.Add(subtitle, 0, 2);
        root.Controls.Add(hint, 0, 3);
        Controls.Add(root);
    }
}