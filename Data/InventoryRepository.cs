using Microsoft.Data.Sqlite;
using GlowvaERP.Models;

namespace GlowvaERP.Data;

public sealed class InventoryRepository
{
    public IReadOnlyList<InventoryStock> GetStock(string? search = null)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                p.id,
                p.code,
                p.name,
                COALESCE(p.category, '') AS category,
                p.opening_stock,
                COALESCE((SELECT SUM(pi.quantity)
                          FROM purchase_items pi
                          WHERE pi.product_id = p.id), 0) AS purchased,
                COALESCE((SELECT SUM(oi.quantity)
                          FROM order_items oi
                          WHERE oi.product_id = p.id), 0) AS sold,
                COALESCE((SELECT SUM(pri.quantity)
                          FROM purchase_return_items pri
                          WHERE pri.product_id = p.id), 0) AS purchase_returns,
                COALESCE((SELECT SUM(sri.quantity)
                          FROM sales_return_items sri
                          WHERE sri.product_id = p.id), 0) AS sales_returns,
                p.low_stock_threshold
            FROM products p
            WHERE p.is_active = 1
              AND ($search = '' OR p.name LIKE $searchPattern OR p.code LIKE $searchPattern OR COALESCE(p.barcode, '') LIKE $searchPattern)
            ORDER BY p.id ASC;
            """;
        var searchText = search?.Trim() ?? "";
        command.Parameters.AddWithValue("$search", searchText);
        command.Parameters.AddWithValue("$searchPattern", $"%{searchText}%");

        using var reader = command.ExecuteReader();
        var result = new List<InventoryStock>();
        while (reader.Read())
        {
            result.Add(new InventoryStock
            {
                ProductId = reader.GetInt64(0),
                Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
                ProductName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Category = reader.IsDBNull(3) ? "" : reader.GetString(3),
                OpeningStock = ToDecimal(reader, 4),
                Purchased = ToDecimal(reader, 5),
                Sold = ToDecimal(reader, 6),
                PurchaseReturns = ToDecimal(reader, 7),
                SalesReturns = ToDecimal(reader, 8),
                LowStockThreshold = ToDecimal(reader, 9)
            });
        }
        return result;
    }

    public IReadOnlyList<InventoryLedgerRow> GetLedger(long productId, int limit = 100)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, movement_date, movement_type, reference_type, reference_id,
                   quantity_in, quantity_out, unit_cost, COALESCE(notes, '')
            FROM inventory_ledger
            WHERE product_id = $productId
            ORDER BY movement_date DESC, id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$productId", productId);
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var rows = new List<InventoryLedgerRow>();
        while (reader.Read())
        {
            rows.Add(new InventoryLedgerRow
            {
                Id = reader.GetInt64(0),
                MovementDate = reader.IsDBNull(1) ? "" : reader.GetString(1),
                MovementType = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ReferenceType = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ReferenceId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                QuantityIn = ToDecimal(reader, 5),
                QuantityOut = ToDecimal(reader, 6),
                UnitCost = ToDecimal(reader, 7),
                Notes = reader.IsDBNull(8) ? "" : reader.GetString(8)
            });
        }
        return rows;
    }

    public decimal GetCurrentStock(long productId)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.opening_stock
                 + COALESCE((SELECT SUM(pi.quantity) FROM purchase_items pi WHERE pi.product_id = p.id), 0)
                 - COALESCE((SELECT SUM(oi.quantity) FROM order_items oi WHERE oi.product_id = p.id), 0)
                 - COALESCE((SELECT SUM(pri.quantity) FROM purchase_return_items pri WHERE pri.product_id = p.id), 0)
                 + COALESCE((SELECT SUM(sri.quantity) FROM sales_return_items sri WHERE sri.product_id = p.id), 0)
            FROM products p
            WHERE p.id = $productId;
            """;
        command.Parameters.AddWithValue("$productId", productId);
        return Convert.ToDecimal(command.ExecuteScalar() ?? 0m);
    }

    // Uploaded Repositories source compatibility: stock summary and ledger DTOs.
    // These wrappers keep the useful API from the uploaded source without adding
    // duplicate repository classes or moving the existing Data layer.
    public IReadOnlyList<StockRow> GetStockRows(string search = "")
    {
        return GetStock(search)
            .Select(x => new StockRow
            {
                ProductId = x.ProductId,
                Code = x.Code,
                ProductName = x.ProductName,
                Category = x.Category,
                OpeningStock = x.OpeningStock,
                Purchased = x.Purchased,
                Sold = x.Sold,
                PurchaseReturns = x.PurchaseReturns,
                SalesReturns = x.SalesReturns,
                CurrentStock = x.CurrentStock,
                IsLowStock = x.IsLowStock
            })
            .ToList();
    }

    public IReadOnlyList<LedgerRow> GetLedgerRows(long productId)
    {
        return GetLedger(productId)
            .Select(x => new LedgerRow
            {
                MovementDate = x.MovementDate,
                MovementType = x.MovementType,
                ReferenceType = x.ReferenceType,
                ReferenceId = x.ReferenceId?.ToString() ?? "",
                QuantityIn = x.QuantityIn,
                QuantityOut = x.QuantityOut,
                UnitCost = x.UnitCost,
                Notes = x.Notes
            })
            .ToList();
    }

    private static decimal ToDecimal(SqliteDataReader reader, int index)
        => reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index));
}

public sealed class InventoryLedgerRow
{
    public long Id { get; set; }
    public string MovementDate { get; set; } = "";
    public string MovementType { get; set; } = "";
    public string ReferenceType { get; set; } = "";
    public long? ReferenceId { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal UnitCost { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class StockRow
{
    public long ProductId { get; init; }
    public string Code { get; init; } = "";
    public string ProductName { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal OpeningStock { get; init; }
    public decimal Purchased { get; init; }
    public decimal Sold { get; init; }
    public decimal PurchaseReturns { get; init; }
    public decimal SalesReturns { get; init; }
    public decimal CurrentStock { get; init; }
    public bool IsLowStock { get; init; }
}

public sealed class LedgerRow
{
    public string MovementDate { get; init; } = "";
    public string MovementType { get; init; } = "";
    public string ReferenceType { get; init; } = "";
    public string ReferenceId { get; init; } = "";
    public decimal QuantityIn { get; init; }
    public decimal QuantityOut { get; init; }
    public decimal UnitCost { get; init; }
    public string Notes { get; init; } = "";
}
