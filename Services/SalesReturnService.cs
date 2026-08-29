using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Services;

public sealed class SalesReturnService
{
    public long SaveReturn(long orderId, long? customerId, string? reason,
        IReadOnlyCollection<SalesReturnItemDraft> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("لا يمكن حفظ مرتجع بدون أصناف.");

        if (items.Any(x => x.ReturnQuantity <= 0 || x.ReturnQuantity > x.AvailableToReturn))
            throw new InvalidOperationException("كمية المرتجع غير صحيحة لبعض الأصناف.");

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var returnNumber = GetNextNumber(connection, transaction, "sales_returns", "return_number");
            var returnDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var total = items.Sum(x => x.Total);

            using var returnCommand = connection.CreateCommand();
            returnCommand.Transaction = transaction;
            returnCommand.CommandText = """
                INSERT INTO sales_returns
                    (return_number, return_date, order_id, customer_id, reason, total)
                VALUES
                    ($number, $date, $orderId, $customerId, $reason, $total);
                SELECT last_insert_rowid();
                """;
            returnCommand.Parameters.AddWithValue("$number", returnNumber);
            returnCommand.Parameters.AddWithValue("$date", returnDate);
            returnCommand.Parameters.AddWithValue("$orderId", orderId);
            returnCommand.Parameters.AddWithValue("$customerId", customerId ?? (object)DBNull.Value);
            returnCommand.Parameters.AddWithValue("$reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason.Trim());
            returnCommand.Parameters.AddWithValue("$total", total);
            var returnId = Convert.ToInt64(returnCommand.ExecuteScalar());

            foreach (var item in items.Where(x => x.ReturnQuantity > 0))
            {
                using var itemCommand = connection.CreateCommand();
                itemCommand.Transaction = transaction;
                itemCommand.CommandText = """
                    INSERT INTO sales_return_items
                        (sales_return_id, order_item_id, product_id, quantity, unit_price, cost_price)
                    VALUES
                        ($returnId, $orderItemId, $productId, $quantity, $unitPrice, $costPrice);
                    """;
                itemCommand.Parameters.AddWithValue("$returnId", returnId);
                itemCommand.Parameters.AddWithValue("$orderItemId", item.OrderItemId);
                itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
                itemCommand.Parameters.AddWithValue("$quantity", item.ReturnQuantity);
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
                        ($productId, $date, 'مرتجع بيع', 'sales_return', $referenceId,
                         $quantityIn, 0, $unitCost, $notes);
                    """;
                ledgerCommand.Parameters.AddWithValue("$productId", item.ProductId);
                ledgerCommand.Parameters.AddWithValue("$date", returnDate);
                ledgerCommand.Parameters.AddWithValue("$referenceId", returnId);
                ledgerCommand.Parameters.AddWithValue("$quantityIn", item.ReturnQuantity);
                ledgerCommand.Parameters.AddWithValue("$unitCost", item.CostPrice);
                ledgerCommand.Parameters.AddWithValue("$notes", $"مرتجع مبيعات رقم {returnNumber}");
                ledgerCommand.ExecuteNonQuery();
            }

            using (var cashCommand = connection.CreateCommand())
            {
                cashCommand.Transaction = transaction;
                cashCommand.CommandText = """
                    INSERT INTO cash_transactions
                        (transaction_date, transaction_type, reference_type, reference_id,
                         amount_in, amount_out, notes)
                    SELECT $date, 'مرتجع مبيعات', 'sales_return', $referenceId, 0, $amount, $notes
                    WHERE EXISTS (
                        SELECT 1 FROM orders WHERE id = $orderId AND payment_status = 'مدفوع'
                    );
                    """;
                cashCommand.Parameters.AddWithValue("$date", returnDate);
                cashCommand.Parameters.AddWithValue("$referenceId", returnId);
                cashCommand.Parameters.AddWithValue("$amount", total);
                cashCommand.Parameters.AddWithValue("$notes", $"رد قيمة مرتجع مبيعات رقم {returnNumber}");
                cashCommand.Parameters.AddWithValue("$orderId", orderId);
                cashCommand.ExecuteNonQuery();
            }

            if (customerId.HasValue && total > 0)
            {
                using var accountCommand = connection.CreateCommand();
                accountCommand.Transaction = transaction;
                accountCommand.CommandText = """
                    INSERT INTO account_transactions
                        (transaction_date, account_type, party_id, reference_type, reference_id, debit, credit, notes)
                    VALUES
                        ($date, 'customer', $partyId, 'sales_return', $referenceId, 0, $credit, $notes);
                    """;
                accountCommand.Parameters.AddWithValue("$date", returnDate);
                accountCommand.Parameters.AddWithValue("$partyId", customerId.Value);
                accountCommand.Parameters.AddWithValue("$referenceId", returnId);
                accountCommand.Parameters.AddWithValue("$credit", total);
                accountCommand.Parameters.AddWithValue("$notes", $"مرتجع مبيعات رقم {returnNumber}");
                accountCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return returnId;
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
}
