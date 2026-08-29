using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace GlowvaERP.Data;

public static class Database
{
    private const string DatabaseFileName = "glowva_erp.db";

    public static string DatabasePath
    {
        get
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GlowvaERP");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, DatabaseFileName);
        }
    }

    public static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    public static void Initialize()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    barcode TEXT,
    category TEXT,
    sell_price REAL NOT NULL DEFAULT 0,
    buy_price REAL NOT NULL DEFAULT 0,
    opening_stock REAL NOT NULL DEFAULT 0,
    low_stock_threshold REAL NOT NULL DEFAULT 5,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS customers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    phone TEXT,
    phone2 TEXT,
    address TEXT,
    notes TEXT,
    opening_balance REAL NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS suppliers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    phone TEXT,
    contact_info TEXT,
    address TEXT,
    notes TEXT,
    opening_balance REAL NOT NULL DEFAULT 0,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    invoice_number INTEGER NOT NULL UNIQUE,
    order_date TEXT NOT NULL,
    customer_id INTEGER,
    payment_status TEXT NOT NULL DEFAULT 'مدفوع',
    order_status TEXT NOT NULL DEFAULT 'مكتمل',
    discount REAL NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(customer_id) REFERENCES customers(id)
);

CREATE TABLE IF NOT EXISTS order_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity REAL NOT NULL,
    unit_price REAL NOT NULL,
    cost_price REAL NOT NULL DEFAULT 0,
    discount REAL NOT NULL DEFAULT 0,
    FOREIGN KEY(order_id) REFERENCES orders(id) ON DELETE CASCADE,
    FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS purchases (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    invoice_number INTEGER NOT NULL UNIQUE,
    purchase_date TEXT NOT NULL,
    supplier_id INTEGER,
    supplier_invoice_number TEXT,
    payment_status TEXT NOT NULL DEFAULT 'مدفوع',
    discount REAL NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(supplier_id) REFERENCES suppliers(id)
);

CREATE TABLE IF NOT EXISTS purchase_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    purchase_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity REAL NOT NULL,
    unit_price REAL NOT NULL,
    discount REAL NOT NULL DEFAULT 0,
    FOREIGN KEY(purchase_id) REFERENCES purchases(id) ON DELETE CASCADE,
    FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS sales_returns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    return_number INTEGER NOT NULL UNIQUE,
    return_date TEXT NOT NULL,
    order_id INTEGER NOT NULL,
    customer_id INTEGER,
    reason TEXT,
    total REAL NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(order_id) REFERENCES orders(id),
    FOREIGN KEY(customer_id) REFERENCES customers(id)
);

CREATE TABLE IF NOT EXISTS sales_return_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    sales_return_id INTEGER NOT NULL,
    order_item_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity REAL NOT NULL,
    unit_price REAL NOT NULL,
    cost_price REAL NOT NULL DEFAULT 0,
    FOREIGN KEY(sales_return_id) REFERENCES sales_returns(id) ON DELETE CASCADE,
    FOREIGN KEY(order_item_id) REFERENCES order_items(id),
    FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS purchase_returns (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    return_number INTEGER NOT NULL UNIQUE,
    return_date TEXT NOT NULL,
    purchase_id INTEGER NOT NULL,
    supplier_id INTEGER,
    reason TEXT,
    total REAL NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(purchase_id) REFERENCES purchases(id),
    FOREIGN KEY(supplier_id) REFERENCES suppliers(id)
);

CREATE TABLE IF NOT EXISTS purchase_return_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    purchase_return_id INTEGER NOT NULL,
    purchase_item_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity REAL NOT NULL,
    unit_price REAL NOT NULL,
    FOREIGN KEY(purchase_return_id) REFERENCES purchase_returns(id) ON DELETE CASCADE,
    FOREIGN KEY(purchase_item_id) REFERENCES purchase_items(id),
    FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS inventory_ledger (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL,
    movement_date TEXT NOT NULL,
    movement_type TEXT NOT NULL,
    reference_type TEXT,
    reference_id INTEGER,
    quantity_in REAL NOT NULL DEFAULT 0,
    quantity_out REAL NOT NULL DEFAULT 0,
    unit_cost REAL NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS cash_transactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transaction_date TEXT NOT NULL,
    transaction_type TEXT NOT NULL,
    reference_type TEXT,
    reference_id INTEGER,
    amount_in REAL NOT NULL DEFAULT 0,
    amount_out REAL NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS account_transactions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    transaction_date TEXT NOT NULL,
    account_type TEXT NOT NULL,
    party_id INTEGER,
    reference_type TEXT,
    reference_id INTEGER,
    debit REAL NOT NULL DEFAULT 0,
    credit REAL NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS profit_payouts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    payout_number INTEGER NOT NULL UNIQUE,
    payout_date TEXT NOT NULL,
    amount REAL NOT NULL,
    reason TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS app_settings (
    key TEXT PRIMARY KEY,
    value TEXT
);

CREATE INDEX IF NOT EXISTS idx_inventory_ledger_product_date
    ON inventory_ledger(product_id, movement_date, id);
CREATE INDEX IF NOT EXISTS idx_orders_customer_date
    ON orders(customer_id, order_date, id);
CREATE INDEX IF NOT EXISTS idx_purchases_supplier_date
    ON purchases(supplier_id, purchase_date, id);
CREATE INDEX IF NOT EXISTS idx_cash_transactions_date
    ON cash_transactions(transaction_date, id);
CREATE INDEX IF NOT EXISTS idx_account_transactions_party
    ON account_transactions(account_type, party_id, transaction_date, id);
";
        command.ExecuteNonQuery();
    }
}
