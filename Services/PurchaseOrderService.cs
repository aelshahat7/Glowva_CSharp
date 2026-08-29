using Microsoft.Data.Sqlite;
using GlowvaERP.Data;
using GlowvaERP.Models;

namespace GlowvaERP.Services;

public sealed class PurchaseOrderService
{
    public long SavePurchase(
        long? supplierId,
        string paymentStatus,
        string? supplierInvoiceNumber,
        decimal discount,
        string? notes,
        IReadOnlyCollection<PurchaseOrderItemDraft> items)
    {
        if (items.Count == 0)
            throw new InvalidOperationException("لا يمكن حفظ فاتورة شراء بدون أصناف.");

        if (!PaymentMethods.IsValid(paymentStatus))
            throw new InvalidOperationException($"طريقة الدفع غير معتمدة: {paymentStatus}");

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var invoiceNumber = GetNextNumber(connection, transaction, "purchases", "invoice_number");
            var purchaseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var subtotal = items.Sum(x => x.Total);
            var total = Math.Max(0m, subtotal - discount);

            using (var purchaseCommand = connection.CreateCommand())
            {
                purchaseCommand.Transaction = transaction;
                purchaseCommand.CommandText = """
                    INSERT INTO purchases
                        (invoice_number, purchase_date, supplier_id, supplier_invoice_number, payment_status, discount, notes)
                    VALUES
                        ($invoiceNumber, $purchaseDate, $supplierId, $supplierInvoiceNumber, $paymentStatus, $discount, $notes);
                    SELECT last_insert_rowid();
                    """;
                purchaseCommand.Parameters.AddWithValue("$invoiceNumber", invoiceNumber);
                purchaseCommand.Parameters.AddWithValue("$purchaseDate", purchaseDate);
                purchaseCommand.Parameters.AddWithValue("$supplierId", supplierId ?? (object)DBNull.Value);
                purchaseCommand.Parameters.AddWithValue("$supplierInvoiceNumber", string.IsNullOrWhiteSpace(supplierInvoiceNumber) ? DBNull.Value : supplierInvoiceNumber.Trim());
                purchaseCommand.Parameters.AddWithValue("$paymentStatus", paymentStatus);
                purchaseCommand.Parameters.AddWithValue("$discount", discount);
                purchaseCommand.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());

                var purchaseId = Convert.ToInt64(purchaseCommand.ExecuteScalar());

                foreach (var item in items)
                {
                    using var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = """
                        INSERT INTO purchase_items
                            (purchase_id, product_id, quantity, unit_price, discount)
                        VALUES
                            ($purchaseId, $productId, $quantity, $unitPrice, 0);
                        """;
                    itemCommand.Parameters.AddWithValue("$purchaseId", purchaseId);
                    itemCommand.Parameters.AddWithValue("$productId", item.ProductId);
                    itemCommand.Parameters.AddWithValue("$quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("$unitPrice", item.UnitPrice);
                    itemCommand.ExecuteNonQuery();

                    using var ledgerCommand = connection.CreateCommand();
                    ledgerCommand.Transaction = transaction;
                    ledgerCommand.CommandText = """
                        INSERT INTO inventory_ledger
                            (product_id, movement_date, movement_type, reference_type, reference_id,
                             quantity_in, quantity_out, unit_cost, notes)
                        VALUES
                            ($productId, $movementDate, 'شراء', 'purchase', $referenceId,
                             $quantityIn, 0, $unitCost, $notes);
                        """;
                    ledgerCommand.Parameters.AddWithValue("$productId", item.ProductId);
                    ledgerCommand.Parameters.AddWithValue("$movementDate", purchaseDate);
                    ledgerCommand.Parameters.AddWithValue("$referenceId", purchaseId);
                    ledgerCommand.Parameters.AddWithValue("$quantityIn", item.Quantity);
                    ledgerCommand.Parameters.AddWithValue("$unitCost", item.UnitPrice);
                    ledgerCommand.Parameters.AddWithValue("$notes", $"فاتورة شراء رقم {invoiceNumber}");
                    ledgerCommand.ExecuteNonQuery();
                }

                if (PaymentMethods.IsImmediate(paymentStatus) && total > 0)
                {
                    InsertCash(connection, transaction, purchaseDate, purchaseId, total,
                        paymentStatus, $"فاتورة شراء رقم {invoiceNumber}");
                }
                else if (paymentStatus == PaymentMethods.Credit && supplierId.HasValue && total > 0)
                {
                    InsertSupplierDebt(connection, transaction, purchaseDate, supplierId.Value, purchaseId, total,
                        $"فاتورة شراء آجلة رقم {invoiceNumber}");
                }

                transaction.Commit();
                return purchaseId;
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

    private static void InsertCash(SqliteConnection connection, SqliteTransaction transaction,
        string date, long purchaseId, decimal amount, string paymentMethod, string notes)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO cash_transactions
                (transaction_date, transaction_type, reference_type, reference_id, amount_in, amount_out, notes)
            VALUES
                ($date, $type, 'purchase', $referenceId, 0, $amountOut, $notes);
            """;
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$type", $"مشتريات - {paymentMethod}");
        command.Parameters.AddWithValue("$referenceId", purchaseId);
        command.Parameters.AddWithValue("$amountOut", amount);
        command.Parameters.AddWithValue("$notes", notes);
        command.ExecuteNonQuery();
    }

    private static void InsertSupplierDebt(SqliteConnection connection, SqliteTransaction transaction,
        string date, long supplierId, long purchaseId, decimal amount, string notes)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO account_transactions
                (transaction_date, account_type, party_id, reference_type, reference_id, debit, credit, notes)
            VALUES
                ($date, 'supplier', $partyId, 'purchase', $referenceId, 0, $credit, $notes);
            """;
        command.Parameters.AddWithValue("$date", date);
        command.Parameters.AddWithValue("$partyId", supplierId);
        command.Parameters.AddWithValue("$referenceId", purchaseId);
        command.Parameters.AddWithValue("$credit", amount);
        command.Parameters.AddWithValue("$notes", notes);
        command.ExecuteNonQuery();
    }
}
