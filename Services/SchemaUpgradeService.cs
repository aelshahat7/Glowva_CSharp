using GlowvaERP.Data;
using Microsoft.Data.Sqlite;

namespace GlowvaERP.Services;

public static class SchemaUpgradeService
{
    public static void Apply()
    {
        using var c = Database.OpenConnection();

        // Base tables that may not exist on older databases must be created
        // before any ALTER TABLE statements target them.
        Exec(c,"CREATE TABLE IF NOT EXISTS users(id INTEGER PRIMARY KEY AUTOINCREMENT,username TEXT NOT NULL UNIQUE,display_name TEXT NOT NULL,password_hash TEXT NOT NULL,is_admin INTEGER NOT NULL DEFAULT 0,is_active INTEGER NOT NULL DEFAULT 1,permissions TEXT NOT NULL DEFAULT '',created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS expenses (id INTEGER PRIMARY KEY AUTOINCREMENT, expense_date TEXT NOT NULL, category TEXT NOT NULL, amount REAL NOT NULL DEFAULT 0, notes TEXT, created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS app_settings (key TEXT PRIMARY KEY,value TEXT);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS warehouses(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL UNIQUE,notes TEXT,is_active INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS warehouse_stock(id INTEGER PRIMARY KEY AUTOINCREMENT,warehouse_id INTEGER NOT NULL,product_id INTEGER NOT NULL,quantity REAL NOT NULL DEFAULT 0,UNIQUE(warehouse_id,product_id));",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS payment_transactions(id INTEGER PRIMARY KEY AUTOINCREMENT,transaction_date TEXT NOT NULL,party_type TEXT,party_id INTEGER,amount REAL NOT NULL,method TEXT NOT NULL,reference_type TEXT,reference_id INTEGER,notes TEXT,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS audit_log(id INTEGER PRIMARY KEY AUTOINCREMENT,user_id INTEGER,action TEXT NOT NULL,entity_type TEXT,entity_id INTEGER,details TEXT,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);",false);
        Exec(c,"CREATE TABLE IF NOT EXISTS print_settings(id INTEGER PRIMARY KEY CHECK(id=1),printer_name TEXT,invoice_width TEXT NOT NULL DEFAULT '80mm',paper_size TEXT NOT NULL DEFAULT 'A4',footer TEXT);",false);

        Exec(c,"ALTER TABLE purchases ADD COLUMN purchase_status TEXT NOT NULL DEFAULT 'مكتمل';",true);
        Exec(c,"ALTER TABLE order_items ADD COLUMN is_return_adjustment INTEGER NOT NULL DEFAULT 0;",true);
        Exec(c,"ALTER TABLE purchase_items ADD COLUMN is_return_adjustment INTEGER NOT NULL DEFAULT 0;",true);
        Exec(c,"ALTER TABLE inventory_ledger ADD COLUMN warehouse_id INTEGER;",true);
        Exec(c,"ALTER TABLE users ADD COLUMN permissions TEXT NOT NULL DEFAULT '';",true);

        Exec(c,"CREATE INDEX IF NOT EXISTS idx_expenses_date ON expenses(expense_date,id);",false);
        Exec(c,"CREATE INDEX IF NOT EXISTS idx_warehouse_stock_product ON warehouse_stock(product_id,warehouse_id);",false);
        Exec(c,"CREATE INDEX IF NOT EXISTS idx_payment_transactions_party ON payment_transactions(party_type,party_id,transaction_date,id);",false);
        Exec(c,"CREATE INDEX IF NOT EXISTS idx_audit_log_date ON audit_log(created_at,id);",false);
        Exec(c,"INSERT OR IGNORE INTO warehouses(name,notes,is_active) VALUES('المخزن الرئيسي','المخزن الافتراضي',1);",false);
        Exec(c,"CREATE INDEX IF NOT EXISTS idx_sales_returns_date ON sales_returns(return_date,id);",false);
        Exec(c,"CREATE INDEX IF NOT EXISTS idx_purchase_returns_date ON purchase_returns(return_date,id);",false);

        Exec(c,"CREATE TRIGGER IF NOT EXISTS trg_sales_return_adjustment AFTER INSERT ON sales_return_items BEGIN INSERT INTO order_items(order_id,product_id,quantity,unit_price,cost_price,discount,is_return_adjustment) SELECT order_id,NEW.product_id,-NEW.quantity,NEW.unit_price,NEW.cost_price,0,1 FROM orders JOIN order_items ON order_items.id=NEW.order_item_id WHERE order_items.order_id=orders.id; END;",false);
        Exec(c,"CREATE TRIGGER IF NOT EXISTS trg_purchase_return_adjustment AFTER INSERT ON purchase_return_items BEGIN INSERT INTO purchase_items(purchase_id,product_id,quantity,unit_price,discount,is_return_adjustment) SELECT purchase_id,NEW.product_id,-NEW.quantity,NEW.unit_price,0,1 FROM purchases JOIN purchase_items ON purchase_items.id=NEW.purchase_item_id WHERE purchase_items.id=NEW.purchase_item_id; END;",false);

        SeedAdmin(c);
    }

    private static void SeedAdmin(SqliteConnection c)
    {
        using var check = c.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM users;";
        var count = Convert.ToInt64(check.ExecuteScalar());
        if (count > 0) return;

        using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO users(username,display_name,password_hash,is_admin,is_active,permissions) VALUES($u,$d,$p,1,1,'*');";
        cmd.Parameters.AddWithValue("$u", "admin");
        cmd.Parameters.AddWithValue("$d", "مدير النظام");
        cmd.Parameters.AddWithValue("$p", AuthService.HashPassword("admin"));
        cmd.ExecuteNonQuery();
    }

    private static void Exec(SqliteConnection c, string sql, bool ignoreDuplicate)
    {
        try
        {
            using var x = c.CreateCommand();
            x.CommandText = sql;
            x.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ignoreDuplicate && ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase)) { }
    }
}
