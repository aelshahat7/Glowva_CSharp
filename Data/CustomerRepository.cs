using Microsoft.Data.Sqlite;
using GlowvaERP.Models;

namespace GlowvaERP.Data;

public sealed class CustomerRepository
{
    public IReadOnlyList<Customer> GetAll(string? search = null, bool activeOnly = false)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("(name LIKE $search OR code LIKE $search OR phone LIKE $search)");
            command.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
        }
        if (activeOnly) conditions.Add("is_active = 1");
        command.CommandText = $"""
            SELECT id, code, name, phone, phone2, address, notes, opening_balance, is_active
            FROM customers
            {(conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions))}
            ORDER BY id ASC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<Customer>();
        while (reader.Read()) result.Add(Map(reader));
        return result;
    }

    public string GenerateNextCode()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(id), 0) + 1 FROM customers;";
        return $"C{Convert.ToInt64(command.ExecuteScalar()):00000}";
    }

    public long Add(Customer item)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO customers (code, name, phone, phone2, address, notes, opening_balance, is_active)
            VALUES ($code, $name, $phone, $phone2, $address, $notes, $openingBalance, $isActive);
            SELECT last_insert_rowid();
            """;
        AddParameters(command, item);
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public void Update(Customer item)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE customers SET code=$code, name=$name, phone=$phone, phone2=$phone2,
                address=$address, notes=$notes, opening_balance=$openingBalance, is_active=$isActive
            WHERE id=$id;
            """;
        AddParameters(command, item);
        command.Parameters.AddWithValue("$id", item.Id);
        command.ExecuteNonQuery();
    }

    public void SetActive(long id, bool active)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE customers SET is_active=$active WHERE id=$id;";
        command.Parameters.AddWithValue("$active", active ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, Customer item)
    {
        command.Parameters.AddWithValue("$code", item.Code.Trim());
        command.Parameters.AddWithValue("$name", item.Name.Trim());
        command.Parameters.AddWithValue("$phone", string.IsNullOrWhiteSpace(item.Phone) ? DBNull.Value : item.Phone.Trim());
        command.Parameters.AddWithValue("$phone2", string.IsNullOrWhiteSpace(item.Phone2) ? DBNull.Value : item.Phone2.Trim());
        command.Parameters.AddWithValue("$address", string.IsNullOrWhiteSpace(item.Address) ? DBNull.Value : item.Address.Trim());
        command.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(item.Notes) ? DBNull.Value : item.Notes.Trim());
        command.Parameters.AddWithValue("$openingBalance", item.OpeningBalance);
        command.Parameters.AddWithValue("$isActive", item.IsActive ? 1 : 0);
    }

    private static Customer Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0), Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
        Name = reader.IsDBNull(2) ? "" : reader.GetString(2), Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
        Phone2 = reader.IsDBNull(4) ? "" : reader.GetString(4), Address = reader.IsDBNull(5) ? "" : reader.GetString(5),
        Notes = reader.IsDBNull(6) ? "" : reader.GetString(6), OpeningBalance = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
        IsActive = !reader.IsDBNull(8) && Convert.ToInt64(reader.GetValue(8)) == 1
    };
}
