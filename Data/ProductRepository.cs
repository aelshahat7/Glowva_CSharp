using Microsoft.Data.Sqlite;
using GlowvaERP.Models;

namespace GlowvaERP.Data;

public sealed class ProductRepository
{
    public IReadOnlyList<Product> GetAll(string? search = null, bool activeOnly = false)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();

        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(name LIKE $search OR code LIKE $search OR barcode LIKE $search)");
            command.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
        }

        if (activeOnly)
            conditions.Add("is_active = 1");

        command.CommandText = $"""
            SELECT id, code, name, barcode, category,
                   sell_price, buy_price, opening_stock,
                   low_stock_threshold, is_active
            FROM products
            {(conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions))}
            ORDER BY id ASC;
            """;

        using var reader = command.ExecuteReader();
        var result = new List<Product>();
        while (reader.Read())
            result.Add(Map(reader));
        return result;
    }

    public string GenerateNextCode()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(id), 0) + 1 FROM products;";
        var nextId = Convert.ToInt64(command.ExecuteScalar());
        return $"P{nextId:00000}";
    }

    public long Add(Product product)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO products
                (code, name, barcode, category, sell_price, buy_price,
                 opening_stock, low_stock_threshold, is_active)
            VALUES
                ($code, $name, $barcode, $category, $sellPrice, $buyPrice,
                 $openingStock, $lowStockThreshold, $isActive);
            SELECT last_insert_rowid();
            """;

        AddParameters(command, product);
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public void Update(Product product)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE products
               SET code = $code,
                   name = $name,
                   barcode = $barcode,
                   category = $category,
                   sell_price = $sellPrice,
                   buy_price = $buyPrice,
                   opening_stock = $openingStock,
                   low_stock_threshold = $lowStockThreshold,
                   is_active = $isActive
             WHERE id = $id;
            """;

        AddParameters(command, product);
        command.Parameters.AddWithValue("$id", product.Id);
        command.ExecuteNonQuery();
    }

    public void SetActive(long id, bool active)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE products SET is_active = $active WHERE id = $id;";
        command.Parameters.AddWithValue("$active", active ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, Product product)
    {
        command.Parameters.AddWithValue("$code", product.Code.Trim());
        command.Parameters.AddWithValue("$name", product.Name.Trim());
        command.Parameters.AddWithValue("$barcode", string.IsNullOrWhiteSpace(product.Barcode) ? DBNull.Value : product.Barcode.Trim());
        command.Parameters.AddWithValue("$category", string.IsNullOrWhiteSpace(product.Category) ? DBNull.Value : product.Category.Trim());
        command.Parameters.AddWithValue("$sellPrice", product.SellPrice);
        command.Parameters.AddWithValue("$buyPrice", product.BuyPrice);
        command.Parameters.AddWithValue("$openingStock", product.OpeningStock);
        command.Parameters.AddWithValue("$lowStockThreshold", product.LowStockThreshold);
        command.Parameters.AddWithValue("$isActive", product.IsActive ? 1 : 0);
    }

    private static Product Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
        Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
        Barcode = reader.IsDBNull(3) ? "" : reader.GetString(3),
        Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
        SellPrice = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
        BuyPrice = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)),
        OpeningStock = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
        LowStockThreshold = reader.IsDBNull(8) ? 5 : Convert.ToDecimal(reader.GetValue(8)),
        IsActive = !reader.IsDBNull(9) && Convert.ToInt64(reader.GetValue(9)) == 1
    };
}
