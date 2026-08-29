using GlowvaERP.Data;

namespace GlowvaERP.Services;

public sealed class AppSettingsService
{
    public string? Get(string key, string? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("مفتاح الإعداد مطلوب.", nameof(key));
        using var connection = Database.OpenConnection();
        EnsureTable(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM app_settings WHERE key=$key;";
        command.Parameters.AddWithValue("$key", key.Trim());
        return command.ExecuteScalar() as string ?? defaultValue;
    }

    public void Set(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("مفتاح الإعداد مطلوب.", nameof(key));
        using var connection = Database.OpenConnection();
        EnsureTable(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO app_settings(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value;";
        command.Parameters.AddWithValue("$key", key.Trim());
        command.Parameters.AddWithValue("$value", value ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    public bool GetBool(string key, bool defaultValue = false)
        => bool.TryParse(Get(key), out var value) ? value : defaultValue;

    public void SetBool(string key, bool value) => Set(key, value.ToString());

    private static void EnsureTable(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS app_settings (key TEXT PRIMARY KEY, value TEXT);";
        command.ExecuteNonQuery();
    }
}
