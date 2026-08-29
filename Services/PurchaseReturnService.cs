using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Services;

public sealed class PurchaseReturnService
{
    public long SaveReturn(long purchaseId, long? supplierId, string? reason,
        IReadOnlyCollection<PurchaseReturnItemDraft> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("لا يمكن حفظ مرتجع مشتريات بدون أصناف.");

        if (items.Any(x => x.ReturnQuantity <= 0 || x.ReturnQuantity > x.AvailableToReturn))
            throw new InvalidOperationException("كمية المرتجع غير صحيحة لبعض الأصناف.");

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var returnNumber = GetNextNumber(connection, transaction, "purchase_returns", "return_number");
            var returnDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var total = items.Sum(x => x.Total);

            using var header = connection.CreateCommand();
            header.Transaction = transaction;
            header.CommandText = """
                INSERT INTO purchase_returns
                    (return_number, return_date, purchase_id, supplier_id, reason, total)
                VALUES ($number, $date, $purchaseId, $supplierId, $reason, $total);
                SELECT last_insert_rowid();
                """;
            header.Parameters.AddWithValue("$number", returnNumber);
            header.Parameters.AddWithValue("$date", returnDate);
            header.Parameters.AddWithValue("$purchaseId", purchaseId);
            header.Parameters.AddWithValue("$supplierId", supplierId ?? (object)DBNull.Value);
            header.Parameters.AddWithValue("$reason", string.IsNullOrWhiteSpace(reason) ? DBNull.Value : reason.Trim());
            header.Parameters.AddWithValue("$total", total);
            var returnId = Convert.ToInt64(header.ExecuteScalar());

            foreach (var item in items.Where(x => x.ReturnQuantity > 0))
            {
                using var line = connection.CreateCommand();
                line.Transaction = transaction;
                line.CommandText = """
                    INSERT INTO purchase_return_items
                        (purchase_return_id, purchase_item_id, product_id, quantity, unit_price)
                    VALUES ($returnId, $purchaseItemId, $productId, $quantity, $unitPrice);
                    """;
                line.Parameters.AddWithValue("$returnId", returnId);
                line.Parameters.AddWithValue("$purchaseItemId", item.PurchaseItemId);
                line.Parameters.AddWithValue("$productId", item.ProductId);
                line.Parameters.AddWithValue("$quantity", item.ReturnQuantity);
                line.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
                line.ExecuteNonQuery();

                using var ledger = connection.CreateCommand();
                ledger.Transaction = transaction;
                ledger.CommandText = """
                    INSERT INTO inventory_ledger
                        (product_id, movement_date, movement_type, reference_type, reference_id,
                         quantity_in, quantity_out, unit_cost, notes)
                    VALUES ($productId, $date, 'مرتجع شراء', 'purchase_return', $referenceId,
                            0, $quantityOut, $unitCost, $notes);
                    """;
                ledger.Parameters.AddWithValue("$productId", item.ProductId);
                ledger.Parameters.AddWithValue("$date", returnDate);
                ledger.Parameters.AddWithValue("$referenceId", returnId);
                ledger.Parameters.AddWithValue("$quantityOut", item.ReturnQuantity);
                ledger.Parameters.AddWithValue("$unitCost", item.UnitPrice);
                ledger.Parameters.AddWithValue("$notes", $"مرتجع مشتريات رقم {returnNumber}");
                ledger.ExecuteNonQuery();
            }

            using (var cash = connection.CreateCommand())
            {
                cash.Transaction = transaction;
                cash.CommandText = """
                    INSERT INTO cash_transactions
                        (transaction_date, transaction_type, reference_type, reference_id,
                         amount_in, amount_out, notes)
                    SELECT $date, 'مرتجع مشتريات', 'purchase_return', $referenceId,
                           $amountIn, 0, $notes
                    WHERE EXISTS (
                        SELECT 1 FROM purchases WHERE id = $purchaseId AND payment_status = 'مدفوع'
                    );
                    """;
                cash.Parameters.AddWithValue("$date", returnDate);
                cash.Parameters.AddWithValue("$referenceId", returnId);
                cash.Parameters.AddWithValue("$amountIn", total);
                cash.Parameters.AddWithValue("$notes", $"استرداد قيمة مرتجع مشتريات رقم {returnNumber}");
                cash.Parameters.AddWithValue("$purchaseId", purchaseId);
                cash.ExecuteNonQuery();
            }

            if (supplierId.HasValue && total > 0)
            {
                using var account = connection.CreateCommand();
                account.Transaction = transaction;
                account.CommandText = """
                    INSERT INTO account_transactions
                        (transaction_date, account_type, party_id, reference_type, reference_id,
                         debit, credit, notes)
                    VALUES ($date, 'supplier', $partyId, 'purchase_return', $referenceId,
                            0, $credit, $notes);
                    """;
                account.Parameters.AddWithValue("$date", returnDate);
                account.Parameters.AddWithValue("$partyId", supplierId.Value);
                account.Parameters.AddWithValue("$referenceId", returnId);
                account.Parameters.AddWithValue("$credit", total);
                account.Parameters.AddWithValue("$notes", $"مرتجع مشتريات رقم {returnNumber}");
                account.ExecuteNonQuery();
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

    private static long GetNextNumber(SqliteConnection connection, SqliteTransaction transaction, string table, string column)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COALESCE(MAX({column}), 0) + 1 FROM {table};";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
