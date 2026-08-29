using GlowvaERP.Data;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace GlowvaERP.Services;

/// <summary>
/// استيراد البيانات القديمة من ملفات CSV مصدّرة من جوجل شيتس.
/// آمن للتشغيل أكثر من مرة — الأسماء الموجودة تُتخطى ولا تُكرَّر.
/// </summary>
public sealed class ImportService
{
    public ImportResult ImportProducts(string csvPath)
    {
        var rows = ReadCsv(csvPath);
        var result = new ImportResult { FileName = Path.GetFileName(csvPath) };
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();

        foreach (var (row, lineNumber) in rows.Select((r, i) => (r, i + 2)))
        {
            try
            {
                var name = GetColumn(row, new[] { "اسم الصنف", "name", "Name" });
                if (string.IsNullOrWhiteSpace(name)) { result.Skipped++; continue; }
                using var check = conn.CreateCommand();
                check.Transaction = tx;
                check.CommandText = "SELECT COUNT(1) FROM products WHERE name=$n;";
                check.Parameters.AddWithValue("$n", name.Trim());
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) { result.Skipped++; continue; }

                var category = GetColumn(row, new[] { "الفئة", "category", "Category" });
                var sellPrice = ParseDecimal(GetColumn(row, new[] { "سعر البيع", "sell_price", "SellPrice" }));
                var buyPrice = ParseDecimal(GetColumn(row, new[] { "سعر الشراء", "buy_price", "BuyPrice" }));
                var threshold = ParseDecimal(GetColumn(row, new[] { "حد التنبيه بالمخزون", "low_stock_threshold" }), 5m);
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO products (name, category, sell_price, buy_price, low_stock_threshold, is_active)
                    VALUES ($name, $cat, $sell, $buy, $thresh, 1);";
                cmd.Parameters.AddWithValue("$name", name.Trim());
                cmd.Parameters.AddWithValue("$cat", category?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$sell", sellPrice);
                cmd.Parameters.AddWithValue("$buy", buyPrice);
                cmd.Parameters.AddWithValue("$thresh", threshold);
                cmd.ExecuteNonQuery();
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"سطر {lineNumber}: {ex.Message}"); }
        }
        tx.Commit();
        return result;
    }

    public ImportResult ImportCustomers(string csvPath)
    {
        var rows = ReadCsv(csvPath);
        var result = new ImportResult { FileName = Path.GetFileName(csvPath) };
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();

        foreach (var (row, lineNumber) in rows.Select((r, i) => (r, i + 2)))
        {
            try
            {
                var name = GetColumn(row, new[] { "اسم العميل", "name", "Name" });
                if (string.IsNullOrWhiteSpace(name)) { result.Skipped++; continue; }
                using var check = conn.CreateCommand();
                check.Transaction = tx;
                check.CommandText = "SELECT COUNT(1) FROM customers WHERE name=$n;";
                check.Parameters.AddWithValue("$n", name.Trim());
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) { result.Skipped++; continue; }

                var phone = GetColumn(row, new[] { "رقم الموبايل", "phone", "Phone" });
                var phone2 = GetColumn(row, new[] { "رقم موبايل تاني", "phone2" });
                var address = GetColumn(row, new[] { "العنوان", "address", "Address" });
                var notes = GetColumn(row, new[] { "ملاحظات", "notes", "Notes" });
                var code = GenerateCode(conn, tx, "customers", "C");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO customers (code, name, phone, phone2, address, notes, is_active)
                    VALUES ($code, $name, $phone, $phone2, $addr, $notes, 1);";
                cmd.Parameters.AddWithValue("$code", code);
                cmd.Parameters.AddWithValue("$name", name.Trim());
                cmd.Parameters.AddWithValue("$phone", phone?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$phone2", phone2?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$addr", address?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$notes", notes?.Trim() ?? "");
                cmd.ExecuteNonQuery();
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"سطر {lineNumber}: {ex.Message}"); }
        }
        tx.Commit();
        return result;
    }

    public ImportResult ImportSuppliers(string csvPath)
    {
        var rows = ReadCsv(csvPath);
        var result = new ImportResult { FileName = Path.GetFileName(csvPath) };
        using var conn = Database.OpenConnection();
        using var tx = conn.BeginTransaction();

        foreach (var (row, lineNumber) in rows.Select((r, i) => (r, i + 2)))
        {
            try
            {
                var name = GetColumn(row, new[] { "اسم المورد", "name", "Name" });
                if (string.IsNullOrWhiteSpace(name)) { result.Skipped++; continue; }
                using var check = conn.CreateCommand();
                check.Transaction = tx;
                check.CommandText = "SELECT COUNT(1) FROM suppliers WHERE name=$n;";
                check.Parameters.AddWithValue("$n", name.Trim());
                if (Convert.ToInt32(check.ExecuteScalar()) > 0) { result.Skipped++; continue; }

                var phone = GetColumn(row, new[] { "الموبايل", "phone", "Phone" });
                var contact = GetColumn(row, new[] { "بيانات التواصل", "contact_info" });
                var code = GenerateCode(conn, tx, "suppliers", "S");
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO suppliers (code, name, phone, contact_info, is_active)
                    VALUES ($code, $name, $phone, $contact, 1);";
                cmd.Parameters.AddWithValue("$code", code);
                cmd.Parameters.AddWithValue("$name", name.Trim());
                cmd.Parameters.AddWithValue("$phone", phone?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$contact", contact?.Trim() ?? "");
                cmd.ExecuteNonQuery();
                result.Imported++;
            }
            catch (Exception ex) { result.Errors.Add($"سطر {lineNumber}: {ex.Message}"); }
        }
        tx.Commit();
        return result;
    }

    private static List<Dictionary<string, string>> ReadCsv(string path)
    {
        var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
        if (lines.Length < 2) return new();
        var headers = ParseCsvLine(lines[0]);
        var result = new List<Dictionary<string, string>>();
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < Math.Min(headers.Count, values.Count); j++) row[headers[j].Trim()] = values[j].Trim();
            result.Add(row);
        }
        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuote = false;
        var current = new System.Text.StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') inQuote = !inQuote;
            else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }

    private static string? GetColumn(Dictionary<string, string> row, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
            if (row.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)) return value;
        return null;
    }

    private static decimal ParseDecimal(string? value, decimal fallback = 0m)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var cleaned = value.Replace(",", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;
    }

    private static string GenerateCode(SqliteConnection conn, SqliteTransaction tx, string table, string prefix)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = $"SELECT COALESCE(MAX(CAST(REPLACE(REPLACE(code,$p,''),$pl,'') AS INTEGER)),0)+1 FROM {table};";
        cmd.Parameters.AddWithValue("$p", prefix);
        cmd.Parameters.AddWithValue("$pl", prefix.ToLower());
        long next = Convert.ToInt64(cmd.ExecuteScalar() ?? 1L);
        return $"{prefix}{next:D5}";
    }
}

public sealed class ImportResult
{
    public string FileName { get; init; } = "";
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; } = new();
    public bool HasErrors => Errors.Count > 0;
    public string Summary => $"تم استيراد: {Imported} | تم تخطيه: {Skipped}" + (HasErrors ? $" | أخطاء: {Errors.Count}" : "");
}
