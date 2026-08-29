using Microsoft.Data.Sqlite;
using GlowvaERP.Data;

namespace GlowvaERP.Services;

public sealed class InventoryAdjustmentService
{
    public long Adjust(long productId, decimal quantityDelta, decimal unitCost, string reason, string? notes = null)
    {
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        if (quantityDelta == 0) throw new InvalidOperationException("كمية التسوية يجب ألا تكون صفرًا.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("اكتب سبب التسوية.");

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            using (var exists = connection.CreateCommand())
            {
                exists.Transaction = transaction;
                exists.CommandText = "SELECT COUNT(1) FROM products WHERE id=$id AND is_active=1;";
                exists.Parameters.AddWithValue("$id", productId);
                if (Convert.ToInt32(exists.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("الصنف غير موجود أو غير نشط.");
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var quantityIn = Math.Max(quantityDelta, 0m);
            var quantityOut = Math.Max(-quantityDelta, 0m);

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO inventory_ledger
                    (product_id, movement_date, movement_type, reference_type, reference_id,
                     quantity_in, quantity_out, unit_cost, notes)
                VALUES
                    ($productId, $date, 'تسوية مخزون', 'adjustment', NULL,
                     $quantityIn, $quantityOut, $unitCost, $notes);
                SELECT last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$productId", productId);
            command.Parameters.AddWithValue("$date", now);
            command.Parameters.AddWithValue("$quantityIn", quantityIn);
            command.Parameters.AddWithValue("$quantityOut", quantityOut);
            command.Parameters.AddWithValue("$unitCost", Math.Max(unitCost, 0m));
            command.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? reason.Trim() : $"{reason.Trim()} - {notes.Trim()}");

            var id = Convert.ToInt64(command.ExecuteScalar());
            transaction.Commit();
            return id;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public decimal GetCurrentStock(long productId)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE((SELECT opening_stock FROM products WHERE id=$id),0)
                 + COALESCE((SELECT SUM(quantity_in-quantity_out) FROM inventory_ledger WHERE product_id=$id),0);
            """;
        command.Parameters.AddWithValue("$id", productId);
        return Convert.ToDecimal(command.ExecuteScalar() ?? 0m);
    }
}
