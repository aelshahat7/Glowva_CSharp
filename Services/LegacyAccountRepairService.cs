using GlowvaERP.Data;
using System.Globalization;

namespace GlowvaERP.Services;

/// <summary>
/// يصلح الحركات المحاسبية القديمة التي قد تكون اتخزنت بتنسيق تاريخ غير قياسي.
/// تُشتغل مرة واحدة عند فتح شاشة الحسابات.
/// </summary>
public sealed class LegacyAccountRepairService
{
    private const string SettingKey = "account_repair_v1_done";

    public void EnsureRebuilt()
    {
        using var conn = Database.OpenConnection();

        // فحص لو الإصلاح اتعمل قبل كده
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT value FROM app_settings WHERE key = $k;";
        checkCmd.Parameters.AddWithValue("$k", SettingKey);
        var done = checkCmd.ExecuteScalar()?.ToString();
        if (done == "1") return;

        // إصلاح التواريخ المخزنة كأرقام OLE Automation
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT id, transaction_date FROM account_transactions;";

        var toFix = new List<(long id, string newDate)>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var raw = r.IsDBNull(1) ? null : r.GetValue(1)?.ToString();
            if (raw == null) continue;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
                && serial > 20000 && serial < 80000)
            {
                try
                {
                    var dt = DateTime.FromOADate(serial);
                    toFix.Add((r.GetInt64(0), dt.ToString("yyyy-MM-dd HH:mm:ss")));
                }
                catch { }
            }
        }

        foreach (var (id, newDate) in toFix)
        {
            using var upd = conn.CreateCommand();
            upd.Transaction = tx;
            upd.CommandText = "UPDATE account_transactions SET transaction_date=$d WHERE id=$id;";
            upd.Parameters.AddWithValue("$d", newDate);
            upd.Parameters.AddWithValue("$id", id);
            upd.ExecuteNonQuery();
        }

        using var mark = conn.CreateCommand();
        mark.Transaction = tx;
        mark.CommandText = "INSERT OR REPLACE INTO app_settings (key, value) VALUES ($k, '1');";
        mark.Parameters.AddWithValue("$k", SettingKey);
        mark.ExecuteNonQuery();

        tx.Commit();
    }
}
