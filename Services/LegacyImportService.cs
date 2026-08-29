using Microsoft.Data.Sqlite;
using GlowvaERP.Data;

namespace GlowvaERP.Services;

public sealed class LegacyImportService
{
    public string Import(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("قاعدة البيانات القديمة غير موجودة.", sourcePath);

        var targetPath = Database.DatabasePath;
        var backupPath = targetPath + ".before-import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";

        if (File.Exists(targetPath))
            File.Copy(targetPath, backupPath, overwrite: false);

        using var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly;");
        using var target = Database.OpenConnection();
        source.Open();

        using var transaction = target.BeginTransaction();
        try
        {
            ClearTarget(target, transaction);

            var productMap = ImportProducts(source, target, transaction);
            var customerMap = ImportCustomers(source, target, transaction);
            var supplierMap = ImportSuppliers(source, target, transaction);

            ImportOrders(source, target, transaction, productMap, customerMap);
            ImportPurchases(source, target, transaction, productMap, supplierMap);
            ImportProfitPayouts(source, target, transaction);

            transaction.Commit();
            return backupPath;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void ClearTarget(SqliteConnection target, SqliteTransaction transaction)
    {
        var tables = new[]
        {
            "sales_return_items", "sales_returns",
            "purchase_return_items", "purchase_returns",
            "inventory_ledger", "cash_transactions", "account_transactions",
            "order_items", "orders", "purchase_items", "purchases",
            "profit_payouts", "customers", "suppliers", "products"
        };

        foreach (var table in tables)
        {
            using var command = target.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"DELETE FROM {table};";
            command.ExecuteNonQuery();
        }
    }

    private static Dictionary<long, long> ImportProducts(SqliteConnection source, SqliteConnection target, SqliteTransaction transaction)
    {
        var map = new Dictionary<long, long>();
        if (!TableExists(source, "products"))
            return map;

        using var command = source.CreateCommand();
        command.CommandText = "SELECT id, name, category, sell_price, buy_price, opening_stock, low_stock_threshold, created_at FROM products ORDER BY id;";
        using var reader = command.ExecuteReader();
        var sequence = 1;

        while (reader.Read())
        {
            var oldId = reader.GetInt64(0);
            var name = reader.IsDBNull(1) ? "صنف بدون اسم" : reader.GetString(1);
            var code = $"P{sequence:00000}";

            using var insert = target.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO products
                    (code, name, barcode, category, sell_price, buy_price, opening_stock, low_stock_threshold, is_active, created_at)
                VALUES
                    ($code, $name, NULL, $category, $sellPrice, $buyPrice, $openingStock, $lowStock, 1, $createdAt);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$code", code);
            insert.Parameters.AddWithValue("$name", name);
            insert.Parameters.AddWithValue("$category", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("$sellPrice", ToDecimal(reader, 3));
            insert.Parameters.AddWithValue("$buyPrice", ToDecimal(reader, 4));
            insert.Parameters.AddWithValue("$openingStock", ToDecimal(reader, 5));
            insert.Parameters.AddWithValue("$lowStock", ToDecimal(reader, 6));
            insert.Parameters.AddWithValue("$createdAt", reader.IsDBNull(7) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(7));
            var newId = Convert.ToInt64(insert.ExecuteScalar());
            map[oldId] = newId;
            sequence++;
        }

        return map;
    }

    private static Dictionary<long, long> ImportCustomers(SqliteConnection source, SqliteConnection target, SqliteTransaction transaction)
    {
        var map = new Dictionary<long, long>();
        if (!TableExists(source, "customers"))
            return map;

        using var command = source.CreateCommand();
        command.CommandText = "SELECT id, name, phone, phone2, address, notes, created_at FROM customers ORDER BY id;";
        using var reader = command.ExecuteReader();
        var sequence = 1;

        while (reader.Read())
        {
            var oldId = reader.GetInt64(0);
            var code = $"C{sequence:00000}";

            using var insert = target.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO customers
                    (code, name, phone, phone2, address, notes, opening_balance, is_active, created_at)
                VALUES
                    ($code, $name, $phone, $phone2, $address, $notes, 0, 1, $createdAt);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$code", code);
            insert.Parameters.AddWithValue("$name", reader.IsDBNull(1) ? "عميل بدون اسم" : reader.GetString(1));
            insert.Parameters.AddWithValue("$phone", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("$phone2", reader.IsDBNull(3) ? "" : reader.GetString(3));
            insert.Parameters.AddWithValue("$address", reader.IsDBNull(4) ? "" : reader.GetString(4));
            insert.Parameters.AddWithValue("$notes", reader.IsDBNull(5) ? "" : reader.GetString(5));
            insert.Parameters.AddWithValue("$createdAt", reader.IsDBNull(6) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(6));
            map[oldId] = Convert.ToInt64(insert.ExecuteScalar());
            sequence++;
        }

        return map;
    }

    private static Dictionary<long, long> ImportSuppliers(SqliteConnection source, SqliteConnection target, SqliteTransaction transaction)
    {
        var map = new Dictionary<long, long>();
        if (!TableExists(source, "suppliers"))
            return map;

        using var command = source.CreateCommand();
        command.CommandText = "SELECT id, name, contact_info, created_at FROM suppliers ORDER BY id;";
        using var reader = command.ExecuteReader();
        var sequence = 1;

        while (reader.Read())
        {
            var oldId = reader.GetInt64(0);
            var code = $"S{sequence:00000}";

            using var insert = target.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO suppliers
                    (code, name, phone, contact_info, address, notes, opening_balance, is_active, created_at)
                VALUES
                    ($code, $name, '', $contact, '', '', 0, 1, $createdAt);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$code", code);
            insert.Parameters.AddWithValue("$name", reader.IsDBNull(1) ? "مورد بدون اسم" : reader.GetString(1));
            insert.Parameters.AddWithValue("$contact", reader.IsDBNull(2) ? "" : reader.GetString(2));
            insert.Parameters.AddWithValue("$createdAt", reader.IsDBNull(3) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(3));
            map[oldId] = Convert.ToInt64(insert.ExecuteScalar());
            sequence++;
        }

        return map;
    }

    private static void ImportOrders(
        SqliteConnection source,
        SqliteConnection target,
        SqliteTransaction transaction,
        Dictionary<long, long> productMap,
        Dictionary<long, long> customerMap)
    {
        if (!TableExists(source, "orders"))
            return;

        using var orders = source.CreateCommand();
        orders.CommandText = "SELECT id, order_date, customer_id, payment_status, order_status, discount, created_at FROM orders ORDER BY id;";
        using var orderReader = orders.ExecuteReader();

        while (orderReader.Read())
        {
            var oldOrderId = orderReader.GetInt64(0);
            var invoiceNumber = oldOrderId;
            long? customerId = null;
            if (!orderReader.IsDBNull(2) && customerMap.TryGetValue(orderReader.GetInt64(2), out var mappedCustomer))
                customerId = mappedCustomer;

            using var insertOrder = target.CreateCommand();
            insertOrder.Transaction = transaction;
            insertOrder.CommandText = """
                INSERT INTO orders
                    (invoice_number, order_date, customer_id, payment_status, order_status, discount, notes, created_at)
                VALUES
                    ($invoice, $date, $customer, $payment, $status, $discount, NULL, $createdAt);
                SELECT last_insert_rowid();
                """;
            insertOrder.Parameters.AddWithValue("$invoice", invoiceNumber);
            insertOrder.Parameters.AddWithValue("$date", orderReader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : orderReader.GetString(1));
            insertOrder.Parameters.AddWithValue("$customer", customerId ?? (object)DBNull.Value);
            insertOrder.Parameters.AddWithValue("$payment", orderReader.IsDBNull(3) ? "مدفوع" : orderReader.GetString(3));
            insertOrder.Parameters.AddWithValue("$status", orderReader.IsDBNull(4) ? "مكتمل" : orderReader.GetString(4));
            insertOrder.Parameters.AddWithValue("$discount", ToDecimal(orderReader, 5));
            insertOrder.Parameters.AddWithValue("$createdAt", orderReader.IsDBNull(6) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : orderReader.GetString(6));
            var newOrderId = Convert.ToInt64(insertOrder.ExecuteScalar());

            if (!TableExists(source, "order_items"))
                continue;

            using var items = source.CreateCommand();
            items.CommandText = "SELECT id, product_id, quantity, unit_price FROM order_items WHERE order_id = $orderId ORDER BY id;";
            items.Parameters.AddWithValue("$orderId", oldOrderId);
            using var itemReader = items.ExecuteReader();

            while (itemReader.Read())
            {
                if (!productMap.TryGetValue(itemReader.GetInt64(1), out var newProductId))
                    continue;

                var quantity = ToDecimal(itemReader, 2);
                var unitPrice = ToDecimal(itemReader, 3);
                var costPrice = GetProductBuyPrice(target, transaction, newProductId);

                using var insertItem = target.CreateCommand();
                insertItem.Transaction = transaction;
                insertItem.CommandText = """
                    INSERT INTO order_items
                        (order_id, product_id, quantity, unit_price, cost_price, discount)
                    VALUES
                        ($orderId, $productId, $quantity, $unitPrice, $costPrice, 0);
                    """;
                insertItem.Parameters.AddWithValue("$orderId", newOrderId);
                insertItem.Parameters.AddWithValue("$productId", newProductId);
                insertItem.Parameters.AddWithValue("$quantity", quantity);
                insertItem.Parameters.AddWithValue("$unitPrice", unitPrice);
                insertItem.Parameters.AddWithValue("$costPrice", costPrice);
                insertItem.ExecuteNonQuery();

                InsertInventoryLedger(target, transaction, newProductId,
                    orderReader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : orderReader.GetString(1),
                    "بيع", "order", newOrderId, 0, quantity, costPrice, $"استيراد فاتورة بيع رقم {invoiceNumber}");
            }
        }
    }

    private static void ImportPurchases(
        SqliteConnection source,
        SqliteConnection target,
        SqliteTransaction transaction,
        Dictionary<long, long> productMap,
        Dictionary<long, long> supplierMap)
    {
        if (!TableExists(source, "purchases"))
            return;

        using var purchases = source.CreateCommand();
        purchases.CommandText = "SELECT id, purchase_date, supplier_id, invoice_number, created_at FROM purchases ORDER BY id;";
        using var purchaseReader = purchases.ExecuteReader();

        while (purchaseReader.Read())
        {
            var oldPurchaseId = purchaseReader.GetInt64(0);
            long? supplierId = null;
            if (!purchaseReader.IsDBNull(2) && supplierMap.TryGetValue(purchaseReader.GetInt64(2), out var mappedSupplier))
                supplierId = mappedSupplier;

            using var insertPurchase = target.CreateCommand();
            insertPurchase.Transaction = transaction;
            insertPurchase.CommandText = """
                INSERT INTO purchases
                    (invoice_number, purchase_date, supplier_id, supplier_invoice_number, payment_status, discount, notes, created_at)
                VALUES
                    ($invoice, $date, $supplier, $supplierInvoice, 'مدفوع', 0, NULL, $createdAt);
                SELECT last_insert_rowid();
                """;
            insertPurchase.Parameters.AddWithValue("$invoice", oldPurchaseId);
            insertPurchase.Parameters.AddWithValue("$date", purchaseReader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : purchaseReader.GetString(1));
            insertPurchase.Parameters.AddWithValue("$supplier", supplierId ?? (object)DBNull.Value);
            insertPurchase.Parameters.AddWithValue("$supplierInvoice", purchaseReader.IsDBNull(3) ? "" : purchaseReader.GetString(3));
            insertPurchase.Parameters.AddWithValue("$createdAt", purchaseReader.IsDBNull(4) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : purchaseReader.GetString(4));
            var newPurchaseId = Convert.ToInt64(insertPurchase.ExecuteScalar());

            if (!TableExists(source, "purchase_items"))
                continue;

            using var items = source.CreateCommand();
            items.CommandText = "SELECT product_id, quantity, unit_price FROM purchase_items WHERE purchase_id = $purchaseId ORDER BY id;";
            items.Parameters.AddWithValue("$purchaseId", oldPurchaseId);
            using var itemReader = items.ExecuteReader();

            while (itemReader.Read())
            {
                if (!productMap.TryGetValue(itemReader.GetInt64(0), out var newProductId))
                    continue;

                var quantity = ToDecimal(itemReader, 1);
                var unitPrice = ToDecimal(itemReader, 2);

                using var insertItem = target.CreateCommand();
                insertItem.Transaction = transaction;
                insertItem.CommandText = """
                    INSERT INTO purchase_items
                        (purchase_id, product_id, quantity, unit_price, discount)
                    VALUES
                        ($purchaseId, $productId, $quantity, $unitPrice, 0);
                    """;
                insertItem.Parameters.AddWithValue("$purchaseId", newPurchaseId);
                insertItem.Parameters.AddWithValue("$productId", newProductId);
                insertItem.Parameters.AddWithValue("$quantity", quantity);
                insertItem.Parameters.AddWithValue("$unitPrice", unitPrice);
                insertItem.ExecuteNonQuery();

                InsertInventoryLedger(target, transaction, newProductId,
                    purchaseReader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : purchaseReader.GetString(1),
                    "شراء", "purchase", newPurchaseId, quantity, 0, unitPrice, $"استيراد فاتورة شراء رقم {oldPurchaseId}");
            }
        }
    }

    private static void ImportProfitPayouts(SqliteConnection source, SqliteConnection target, SqliteTransaction transaction)
    {
        if (!TableExists(source, "profit_payouts"))
            return;

        using var command = source.CreateCommand();
        command.CommandText = "SELECT id, payout_date, amount, reason, created_at FROM profit_payouts ORDER BY id;";
        using var reader = command.ExecuteReader();
        var number = 1;

        while (reader.Read())
        {
            using var insert = target.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO profit_payouts
                    (payout_number, payout_date, amount, reason, notes, created_at)
                VALUES
                    ($number, $date, $amount, $reason, NULL, $createdAt);
                """;
            insert.Parameters.AddWithValue("$number", number++);
            insert.Parameters.AddWithValue("$date", reader.IsDBNull(1) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(1));
            insert.Parameters.AddWithValue("$amount", ToDecimal(reader, 2));
            insert.Parameters.AddWithValue("$reason", reader.IsDBNull(3) ? "" : reader.GetString(3));
            insert.Parameters.AddWithValue("$createdAt", reader.IsDBNull(4) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : reader.GetString(4));
            insert.ExecuteNonQuery();
        }
    }

    private static void InsertInventoryLedger(
        SqliteConnection target,
        SqliteTransaction transaction,
        long productId,
        string date,
        string movementType,
        string referenceType,
        long referenceId,
        decimal quantityIn,
        decimal quantityOut,
        decimal unitCost,
        string notes)
    {
        using var command = target.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO inventory_ledger
                (product_id, movement_date, movement_type, reference_type, reference_id,
                 quantity_in, quantity_out, unit_cost, notes)
            VALUES
                ($productId, $date, $type, $referenceType, $referenceId,
                 $quantityIn, $quantityOut, $unitCost, $notes);
            """;
        command.Parameters.AddWithValue("$productId", productId);
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$type", movementType);
        command.Parameters.AddWithValue("$referenceType", referenceType);
        command.Parameters.AddWithValue("$referenceId", referenceId);
        command.Parameters.AddWithValue("$quantityIn", quantityIn);
        command.Parameters.AddWithValue("$quantityOut", quantityOut);
        command.Parameters.AddWithValue("$unitCost", unitCost);
        command.Parameters.AddWithValue("$notes", notes);
        command.ExecuteNonQuery();
    }

    private static decimal GetProductBuyPrice(SqliteConnection target, SqliteTransaction transaction, long productId)
    {
        using var command = target.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT buy_price FROM products WHERE id = $id;";
        command.Parameters.AddWithValue("$id", productId);
        return Convert.ToDecimal(command.ExecuteScalar() ?? 0m);
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type IN ('table','view') AND name = $name;";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static decimal ToDecimal(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return 0m;

        return Convert.ToDecimal(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }
}
