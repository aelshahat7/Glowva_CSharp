using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Services;

public sealed class SalesOrderService
{
    public long SaveOrder(
        long? customerId,
        string paymentStatus,
        decimal discount,
        string? notes,
        IReadOnlyCollection<SalesOrderItemDraft> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("لا يمكن حفظ فاتورة بدون أصناف.");

        if (!PaymentMethods.IsValid(paymentStatus))
            throw new InvalidOperationException($"طريقة الدفع غير معتمدة: {paymentStatus}");

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var invoiceNumber = GetNextNumber(connection, transaction, "orders", "invoice_number");
            var orderDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subtotal = items.Sum(x => x.Total);
            var total = Math.Max(0m, subtotal - discount);

            using (var orderCommand = connection.CreateCommand())
            {
                orderCommand.Transaction = transaction;
                orderCommand.CommandText = """
                    INSERT INTO orders
                        (invoice_number, order_date, customer_id, payment_status, order_status, discount, notes)
                    VALUES
                        ($invoiceNumber, $orderDate, $customerId, $paymentStatus, 'مكتمل', $discount, $notes);
                    SELECT last_insert_rowid();
                    """;
                orderCommand.Parameters.AddWithValue("$invoiceNumber", invoiceNumber);
                orderCommand.Parameters.AddWithValue("$orderDate", orderDate);
                orderCommand.Parameters.AddWithValue("$customerId", customerId ?? (object)DBNull.Value);
                orderCommand.Parameters.AddWithValue("$paymentStatus", paymentStatus);
                orderCommand.Parameters.AddWithValue("$discount", discount);
                orderCommand.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());

                var orderId = Convert.ToInt64(orderCommand.ExecuteScalar());

                foreach (var item in items)
                {
                    var currentStock = GetCurrentStock(connection, transaction, item.ProductId);
                    if (item.Quantity > currentStock)
                        throw new InvalidOperationException($"الرصيد غير كافٍ للصنف: {item.ProductName}. المتاح {currentStock:0.##}");

                    using var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = """
                        INSERT INTO order_items
                            (order_id, product_id, quantity, unit_price, cost_price, discount)
                        VALUES
                            ($orderId, $productId, $quantity, $unitPrice, $costPrice, 0);
                        """;
                    itemCommand.Parameters.AddWithValue("$orderId", orderId);
                    itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
                    itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
                    itemCommand.Parameters.AddWithValue("$costPrice", item.CostPrice);
                    itemCommand.ExecuteNonQuery();

                    using var ledgerCommand = connection.CreateCommand();
                    ledgerCommand.Transaction = transaction;
                    ledgerCommand.CommandText = """
                        INSERT INTO inventory_ledger
                            (product_id, movement_date, movement_type, reference_type, reference_id,
                             quantity_in, quantity_out, unit_cost, notes)
                        VALUES
                            ($productId, $movementDate, 'بيع', 'order', $referenceId,
                             0, $quantityOut, $unitCost, $notes);
                        """;
                    ledgerCommand.Parameters.AddWithValue("$productId", item.ProductId);
                    ledgerCommand.Parameters.AddWithValue("$movementDate", orderDate);
                    ledgerCommand.Parameters.AddWithValue("$referenceId", orderId);
                    ledgerCommand.Parameters.AddWithValue("$quantityOut", item.Quantity);
                    ledgerCommand.Parameters.AddWithValue("$unitCost", item.CostPrice);
                    ledgerCommand.Parameters.AddWithValue("$notes", $"فاتورة بيع رقم {invoiceNumber}");
                    ledgerCommand.ExecuteNonQuery();
                }

                if (PaymentMethods.IsImmediate(paymentStatus) && total > 0)
                {
                    InsertCash(connection, transaction, orderDate, orderId, total,
                        paymentStatus, $"فاتورة بيع رقم {invoiceNumber}");
                }
                else if (paymentStatus == PaymentMethods.Credit && customerId.HasValue && total > 0)
                {
                    InsertCustomerDebt(connection, transaction, orderDate, customerId.Value, orderId, total,
                        $"فاتورة بيع آجلة رقم {invoiceNumber}");
                }

                transaction.Commit();
                return orderId;
            }
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static long GetNextNumber(SqliteConnection connection, SqliteTransaction transaction,
        string table, string column)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX({column}), 0) + 1 FROM {table};";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static decimal GetCurrentStock(SqliteConnection connection, SqliteTransaction transaction, long productId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                COALESCE((SELECT opening_stock FROM products WHERE id = $productId), 0)
                + COALESCE((SELECT SUM(quantity_in - quantity_out)
                            FROM inventory_ledger WHERE product_id = $productId), 0);
            """;
        command.Parameters.AddWithValue("$productId", productId);
        return Convert.ToDecimal(command.ExecuteScalar() ?? 0m);
    }

    private static void InsertCash(SqliteConnection connection, SqliteTransaction transaction,
        string date, long orderId, decimal amount, string paymentMethod, string notes)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO cash_transactions
                (transaction_date, transaction_type, reference_type, reference_id, amount_in, amount_out, notes)
            VALUES
                ($date, $type, 'order', $referenceId, $amountIn, 0, $notes);
            """;
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$type", $"مبيعات - {paymentMethod}");
        command.Parameters.AddWithValue("$referenceId", orderId);
        command.Parameters.AddWithValue("$amountIn", amount);
        command.Parameters.AddWithValue("$notes", notes);
        command.ExecuteNonQuery();
    }

    private static void InsertCustomerDebt(SqliteConnection connection, SqliteTransaction transaction,
        string date, long customerId, long orderId, decimal amount, string notes)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO account_transactions
                (transaction_date, account_type, party_id, reference_type, reference_id, debit, credit, notes)
            VALUES
                ($date, 'customer', $partyId, 'order', $referenceId, $debit, 0, $notes);
            """;
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$partyId", customerId);
        command.Parameters.AddWithValue("$referenceId", orderId);
        command.Parameters.AddWithValue("$debit", amount);
        command.Parameters.AddWithValue("$notes", notes);
        command.ExecuteNonQuery();
    }
}
