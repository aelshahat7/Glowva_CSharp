using Microsoft.Data.Sqlite;
using GlowvaERP.Models;

namespace GlowvaERP.Data;

public sealed class SupplierRepository
{
    public IReadOnlyList<Supplier> GetAll(string? search = null, bool activeOnly = false)
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
            SELECT id, code, name, phone, contact_info, address, notes, opening_balance, is_active
            FROM suppliers
            {(conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions))}
            ORDER BY id ASC;
            """;
        using var reader = command.ExecuteReader();
        var result = new List<Supplier>();
        while (reader.Read()) result.Add(Map(reader));
        return result;
    }

    public string GenerateNextCode()
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(id), 0) + 1 FROM suppliers;";
        return $"S{Convert.ToInt64(command.ExecuteScalar()):00000}";
    }

    public long Add(Supplier item)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO suppliers (code, name, phone, contact_info, address, notes, opening_balance, is_active)
            VALUES ($code, $name, $phone, $contactInfo, $address, $notes, $openingBalance, $isActive);
            SELECT last_insert_rowid();
            """;
        AddParameters(command, item);
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public void Update(Supplier item)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE suppliers SET code=$code, name=$name, phone=$phone, contact_info=$contactInfo,
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
        command.CommandText = "UPDATE suppliers SET is_active=$active WHERE id=$id;";
        command.Parameters.AddWithValue("$active", active ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, Supplier item)
    {
        command.Parameters.AddWithValue("$code", item.Code.Trim());
        command.Parameters.AddWithValue("$name", item.Name.Trim());
        command.Parameters.AddWithValue("$phone", string.IsNullOrWhiteSpace(item.Phone) ? DBNull.Value : item.Phone.Trim());
        command.Parameters.AddWithValue("$contactInfo", string.IsNullOrWhiteSpace(item.ContactInfo) ? DBNull.Value : item.ContactInfo.Trim());
        command.Parameters.AddWithValue("$address", string.IsNullOrWhiteSpace(item.Address) ? DBNull.Value : item.Address.Trim());
        command.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(item.Notes) ? DBNull.Value : item.Notes.Trim());
        command.Parameters.AddWithValue("$openingBalance", item.OpeningBalance);
        command.Parameters.AddWithValue("$isActive", item.IsActive ? 1 : 0);
    }

    private static Supplier Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0), Code = reader.IsDBNull(1) ? "" : reader.GetString(1),
        Name = reader.IsDBNull(2) ? "" : reader.GetString(2), Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
        ContactInfo = reader.IsDBNull(4) ? "" : reader.GetString(4), Address = reader.IsDBNull(5) ? "" : reader.GetString(5),
        Notes = reader.IsDBNull(6) ? "" : reader.GetString(6), OpeningBalance = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
        IsActive = !reader.IsDBNull(8) && Convert.ToInt64(reader.GetValue(8)) == 1
    };
}
