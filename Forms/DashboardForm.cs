using GlowvaERP.Data;
using GlowvaERP.Helpers;

namespace GlowvaERP.Forms;

public sealed class DashboardForm : Form
{
    public DashboardForm()
    {
        Text          = "لوحة التحكم";
        BackColor     = Color.FromArgb(245, 245, 245);
        RightToLeft   = RightToLeft.Yes;
        BuildUi();
    }

    private void BuildUi()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };

        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 60,
            BackColor = Color.FromArgb(27, 94, 32),
        };
        header.Controls.Add(new Label
        {
            Text      = "لوحة التحكم — Glowva ERP",
            Dock      = DockStyle.Fill,
            Font      = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
        });

        var kpiPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 140,
            ColumnCount = 3,
            RowCount    = 2,
            Padding     = new Padding(0, 12, 0, 12),
        };
        for (int i = 0; i < 3; i++)
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        kpiPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var summary = LoadSummary();
        var cards = new[]
        {
            ("إجمالي المبيعات",  summary.TotalSales.ToString("N2"),    Color.FromArgb(21, 101, 192)),
            ("إجمالي المشتريات", summary.TotalPurchases.ToString("N2"), Color.FromArgb(27, 94, 32)),
            ("صافي الربح",       summary.NetProfit.ToString("N2"),      summary.NetProfit >= 0 ? Color.FromArgb(46, 125, 50) : Color.FromArgb(183, 28, 28)),
            ("المصروفات",        summary.TotalExpenses.ToString("N2"),  Color.FromArgb(230, 81, 0)),
            ("أصناف ناقصة",      summary.LowStockCount.ToString(),       summary.LowStockCount > 0 ? Color.FromArgb(183, 28, 28) : Color.FromArgb(46, 125, 50)),
            ("رصيد الخزينة",     summary.CashBalance.ToString("N2"),    summary.CashBalance >= 0 ? Color.FromArgb(21, 101, 192) : Color.FromArgb(183, 28, 28)),
        };

        for (int i = 0; i < cards.Length; i++)
        {
            var (label, value, color) = cards[i];
            var card = new Panel { BackColor = Color.White, Margin = new Padding(8) };
            card.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.FromArgb(224, 224, 224)), 0, 0, card.Width - 1, card.Height - 1);
            card.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(color), 0, 0, 5, card.Height);
            card.Controls.Add(new Label
            {
                Text = label, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(120, 120, 120),
                Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.BottomRight, Padding = new Padding(0, 0, 10, 0)
            });
            card.Controls.Add(new Label
            {
                Text = value, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = color,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 10, 0)
            });
            kpiPanel.Controls.Add(card, i % 3, i / 3);
        }

        if (summary.LowStockCount > 0)
            scroll.Controls.Add(LoadLowStockPanel());

        scroll.Controls.Add(kpiPanel);

        var inner = new Panel { Dock = DockStyle.Fill };
        inner.Controls.Add(scroll);
        inner.Controls.Add(header);
        Controls.Add(inner);
    }

    private Panel LoadLowStockPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 160,
            BackColor = Color.FromArgb(255, 243, 224),
            Padding = new Padding(12),
            Margin = new Padding(0, 12, 0, 0),
        };

        panel.Controls.Add(new Label
        {
            Text = "⚠️  أصناف رصيدها أقل من الحد الأدنى",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(230, 81, 0),
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleRight,
        });

        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT p.name,
                   p.opening_stock + COALESCE(SUM(il.quantity_in - il.quantity_out),0) AS stock,
                   p.low_stock_threshold
            FROM products p
            LEFT JOIN inventory_ledger il ON il.product_id = p.id
            WHERE p.is_active = 1
            GROUP BY p.id
            HAVING stock <= p.low_stock_threshold
            ORDER BY stock
            LIMIT 8;";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            panel.Controls.Add(new Label
            {
                Text = $"• {r.GetString(0)}  —  المتبقي: {Convert.ToDecimal(r.GetValue(1)):N2}  |  الحد: {Convert.ToDecimal(r.GetValue(2)):N2}",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(183, 28, 28),
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleRight,
            });
        }
        return panel;
    }

    private DashboardSummary LoadSummary()
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT
              (SELECT COALESCE(SUM(COALESCE((SELECT SUM(quantity*unit_price) FROM order_items WHERE order_id=o.id),0) - o.discount),0)
               FROM orders o WHERE o.order_status != 'ملغاة') AS total_sales,
              (SELECT COALESCE(SUM(COALESCE((SELECT SUM(quantity*unit_price) FROM purchase_items WHERE purchase_id=p.id),0) - p.discount),0)
               FROM purchases p WHERE p.payment_status != 'ملغاة') AS total_purchases,
              (SELECT COALESCE(SUM(amount),0) FROM expenses) AS total_expenses,
              (SELECT COALESCE(SUM(amount_in - amount_out),0) FROM cash_transactions) AS cash_balance,
              (SELECT COUNT(*) FROM products p
               WHERE p.is_active = 1
               AND p.opening_stock + COALESCE((SELECT SUM(il.quantity_in - il.quantity_out) FROM inventory_ledger il WHERE il.product_id = p.id),0)
                   <= p.low_stock_threshold) AS low_stock_count;";

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return new();
        decimal sales = r.IsDBNull(0) ? 0 : Convert.ToDecimal(r.GetValue(0));
        decimal purchases = r.IsDBNull(1) ? 0 : Convert.ToDecimal(r.GetValue(1));
        decimal expenses = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2));
        decimal cash = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3));
        int lowStock = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4));
        return new DashboardSummary
        {
            TotalSales = sales,
            TotalPurchases = purchases,
            TotalExpenses = expenses,
            NetProfit = sales - purchases - expenses,
            CashBalance = cash,
            LowStockCount = lowStock,
        };
    }

    private sealed class DashboardSummary
    {
        public decimal TotalSales { get; init; }
        public decimal TotalPurchases { get; init; }
        public decimal TotalExpenses { get; init; }
        public decimal NetProfit { get; init; }
        public decimal CashBalance { get; init; }
        public int LowStockCount { get; init; }
    }
}
