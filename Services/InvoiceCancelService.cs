using GlowvaERP.Data;
using GlowvaERP.Models;
using Microsoft.Data.Sqlite;

namespace GlowvaERP.Services;

/// <summary>
/// إلغاء فاتورة: يعكس حركات المخزون والحسابات والخزينة الأصلية داخل Transaction واحدة.
/// يدعم طرق الدفع الخمس المعتمدة في النظام.
/// </summary>
public sealed class InvoiceCancelService
{
    public void Cancel(long invoiceId, bool isSales)
    {
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            EnsurePurchaseStatusColumn(conn, tx);

            if (isSales)
                CancelSalesOrder(conn, tx, invoiceId);
            else
                CancelPurchase(conn, tx, invoiceId);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static void CancelSalesOrder(SqliteConnection conn, SqliteTransaction tx, long orderId)
    {
        var (invNum, paymentStatus, discount, status, customerId) = GetOrderHeader(conn, tx, orderId);
        if (status == "ملغاة")
            throw new InvalidOperationException("الفاتورة ملغاة بالفعل.");
        if (!PaymentMethods.IsValid(paymentStatus))
            throw new InvalidOperationException($"طريقة الدفع غير معتمدة: {paymentStatus}");

        var items = GetOrderItems(conn, tx, orderId);
        if (items.Count == 0)
            throw new InvalidOperationException("الفاتورة لا تحتوي على أصناف أو تم إلغاؤها مسبقًا.");

        var cancelDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        decimal total = Math.Max(0m, items.Sum(x => x.Qty * x.Price - x.Discount) - discount);

        foreach (var item in items)
        {
            Exec(conn, tx, """
                INSERT INTO inventory_ledger
                    (product_id, movement_date, movement_type, reference_type, reference_id,
                     quantity_in, quantity_out, unit_cost, notes)
                VALUES ($pid, $date, 'إلغاء بيع', 'order_cancel', $ref, $in, 0, $cost, $notes);
                """,
                ("$pid", item.ProductId),
                ("$date", cancelDate),
                ("$ref", orderId),
                ("$in", item.Qty),
                ("$cost", item.CostPrice),
                ("$notes", $"إلغاء فاتورة مبيعات رقم {invNum}"));
        }

        if (PaymentMethods.IsImmediate(paymentStatus) && total > 0)
        {
            Exec(conn, tx, """
                INSERT INTO cash_transactions
                    (transaction_date, transaction_type, reference_type, reference_id,
                     amount_in, amount_out, notes)
                VALUES ($date, 'إلغاء مبيعات', 'order_cancel', $ref, 0, $out, $notes);
                """,
                ("$date", cancelDate),
                ("$ref", orderId),
                ("$out", total),
                ("$notes", $"عكس فاتورة مبيعات رقم {invNum}"));
        }
        else if (paymentStatus == PaymentMethods.Credit && customerId.HasValue && total > 0)
        {
            Exec(conn, tx, """
                INSERT INTO account_transactions
                    (transaction_date, account_type, party_id, reference_type, reference_id,
                     debit, credit, notes)
                VALUES ($date, 'customer', $party, 'order_cancel', $ref, 0, $credit, $notes);
                """,
                ("$date", cancelDate),
                ("$party", customerId.Value),
                ("$ref", orderId),
                ("$credit", total),
                ("$notes", $"عكس فاتورة مبيعات آجلة رقم {invNum}"));
        }

        Exec(conn, tx, "UPDATE orders SET order_status = 'ملغاة' WHERE id = $id;",
            ("$id", orderId));
    }

    private static void CancelPurchase(SqliteConnection conn, SqliteTransaction tx, long purchaseId)
    {
        var (invNum, paymentStatus, discount, supplierId, status) = GetPurchaseHeader(conn, tx, purchaseId);
        if (status == "ملغاة")
            throw new InvalidOperationException("الفاتورة ملغاة بالفعل.");
        if (!PaymentMethods.IsValid(paymentStatus))
            throw new InvalidOperationException($"طريقة الدفع غير معتمدة: {paymentStatus}");

        var items = GetPurchaseItems(conn, tx, purchaseId);
        if (items.Count == 0)
            throw new InvalidOperationException("الفاتورة لا تحتوي على أصناف أو تم إلغاؤها مسبقًا.");

        var cancelDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        decimal total = Math.Max(0m, items.Sum(x => x.Qty * x.Price - x.Discount) - discount);

        foreach (var item in items)
        {
            Exec(conn, tx, """
                INSERT INTO inventory_ledger
                    (product_id, movement_date, movement_type, reference_type, reference_id,
                     quantity_in, quantity_out, unit_cost, notes)
                VALUES ($pid, $date, 'إلغاء شراء', 'purchase_cancel', $ref, 0, $out, $cost, $notes);
                """,
                ("$pid", item.ProductId),
                ("$date", cancelDate),
                ("$ref", purchaseId),
                ("$out", item.Qty),
                ("$cost", item.Price),
                ("$notes", $"إلغاء فاتورة شراء رقم {invNum}"));
        }

        if (PaymentMethods.IsImmediate(paymentStatus) && total > 0)
        {
            Exec(conn, tx, """
                INSERT INTO cash_transactions
                    (transaction_date, transaction_type, reference_type, reference_id,
                     amount_in, amount_out, notes)
                VALUES ($date, 'إلغاء مشتريات', 'purchase_cancel', $ref, $in, 0, $notes);
                """,
                ("$date", cancelDate),
                ("$ref", purchaseId),
                ("$in", total),
                ("$notes", $"عكس فاتورة مشتريات رقم {invNum}"));
        }
        else if (paymentStatus == PaymentMethods.Credit && supplierId.HasValue && total > 0)
        {
            Exec(conn, tx, """
                INSERT INTO account_transactions
                    (transaction_date, account_type, party_id, reference_type, reference_id,
                     debit, credit, notes)
                VALUES ($date, 'supplier', $party, 'purchase_cancel', $ref, $debit, 0, $notes);
                """,
                ("$date", cancelDate),
                ("$party", supplierId.Value),
                ("$ref", purchaseId),
                ("$debit", total),
                ("$notes", $"عكس فاتورة مشتريات آجلة رقم {invNum}"));
        }

        Exec(conn, tx, "UPDATE purchases SET purchase_status = 'ملغاة' WHERE id = $id;",
            ("$id", purchaseId));
    }

    private static (long invNum, string payment, decimal discount, string status, long? customerId) GetOrderHeader(
        SqliteConnection c, SqliteTransaction tx, long id)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT invoice_number, payment_status, discount, COALESCE(order_status,'مكتمل'), customer_id FROM orders WHERE id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) throw new InvalidOperationException("الفاتورة غير موجودة.");
        return (
            r.GetInt64(0),
            r.GetString(1),
            r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
            r.IsDBNull(3) ? "مكتمل" : r.GetString(3),
            r.IsDBNull(4) ? null : r.GetInt64(4));
    }

    private static (long invNum, string payment, decimal discount, long? supplierId, string status) GetPurchaseHeader(
        SqliteConnection c, SqliteTransaction tx, long id)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT invoice_number, payment_status, discount, supplier_id, COALESCE(purchase_status,'مكتمل') FROM purchases WHERE id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) throw new InvalidOperationException("الفاتورة غير موجودة.");
        return (
            r.GetInt64(0),
            r.GetString(1),
            r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
            r.IsDBNull(3) ? null : r.GetInt64(3),
            r.IsDBNull(4) ? "مكتمل" : r.GetString(4));
    }

    private sealed record ItemLine(long ProductId, decimal Qty, decimal Price, decimal CostPrice, decimal Discount);

    private static List<ItemLine> GetOrderItems(SqliteConnection c, SqliteTransaction tx, long orderId)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT product_id, quantity, unit_price, cost_price, discount FROM order_items WHERE order_id=$id;";
        cmd.Parameters.AddWithValue("$id", orderId);
        var list = new List<ItemLine>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ItemLine(
                r.GetInt64(0),
                Convert.ToDecimal(r.GetValue(1)),
                Convert.ToDecimal(r.GetValue(2)),
                r.IsDBNull(3) ? 0m : Convert.ToDecimal(r.GetValue(3)),
                r.IsDBNull(4) ? 0m : Convert.ToDecimal(r.GetValue(4))));
        return list;
    }

    private static List<ItemLine> GetPurchaseItems(SqliteConnection c, SqliteTransaction tx, long purchaseId)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT product_id, quantity, unit_price, discount FROM purchase_items WHERE purchase_id=$id;";
        cmd.Parameters.AddWithValue("$id", purchaseId);
        var list = new List<ItemLine>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ItemLine(
                r.GetInt64(0),
                Convert.ToDecimal(r.GetValue(1)),
                Convert.ToDecimal(r.GetValue(2)),
                Convert.ToDecimal(r.GetValue(2)),
                r.IsDBNull(3) ? 0m : Convert.ToDecimal(r.GetValue(3))));
        return list;
    }

    private static void EnsurePurchaseStatusColumn(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var check = connection.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = "PRAGMA table_info(purchases);";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), "purchase_status", StringComparison.OrdinalIgnoreCase))
                return;
        }

        using var alter = connection.CreateCommand();
        alter.Transaction = transaction;
        alter.CommandText = "ALTER TABLE purchases ADD COLUMN purchase_status TEXT NOT NULL DEFAULT 'مكتمل';";
        alter.ExecuteNonQuery();
    }

    private static void Exec(SqliteConnection c, SqliteTransaction tx, string sql,
        params (string name, object? value)[] parms)
    {
        using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in parms)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }
}
