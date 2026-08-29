using GlowvaERP.Data;

namespace GlowvaERP.Services;

public sealed record ExpenseRow(long Id, string Date, string Category, decimal Amount, string Notes);

public sealed class ExpenseService
{
    public void Add(DateTime date, string category, decimal amount, string? notes)
    {
        if (amount <= 0) throw new InvalidOperationException("قيمة المصروف يجب أن تكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(category)) throw new InvalidOperationException("اكتب نوع المصروف.");

        using var c = Database.OpenConnection();
        EnsureTable(c);
        using var tx = c.BeginTransaction();

        using (var cmd = c.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO expenses(expense_date,category,amount,notes) VALUES($date,$category,$amount,$notes);";
            cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$category", category.Trim());
            cmd.Parameters.AddWithValue("$amount", amount);
            cmd.Parameters.AddWithValue("$notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());
            cmd.ExecuteNonQuery();
        }

        using (var cmd = c.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO cash_transactions(transaction_date,transaction_type,reference_type,reference_id,amount_in,amount_out,notes) VALUES($date,'مصروف','expense',(SELECT last_insert_rowid()),0,$amount,$notes);";
            cmd.Parameters.AddWithValue("$date", date.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$amount", amount);
            cmd.Parameters.AddWithValue("$notes", $"مصروف: {category.Trim()}");
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public IReadOnlyList<ExpenseRow> Get(DateTime from, DateTime to)
    {
        using var c = Database.OpenConnection();
        EnsureTable(c);
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id,expense_date,category,amount,COALESCE(notes,'') FROM expenses WHERE date(expense_date) BETWEEN date($from) AND date($to) ORDER BY expense_date DESC,id DESC;";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        using var r = cmd.ExecuteReader();
        var list = new List<ExpenseRow>();
        while (r.Read())
            list.Add(new ExpenseRow(r.GetInt64(0), r.GetString(1), r.GetString(2), Convert.ToDecimal(r.GetValue(3)), r.GetString(4)));
        return list;
    }

    public decimal Total(DateTime from, DateTime to)
    {
        using var c = Database.OpenConnection();
        EnsureTable(c);
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(amount),0) FROM expenses WHERE date(expense_date) BETWEEN date($from) AND date($to);";
        cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
    }

    private static void EnsureTable(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS expenses (id INTEGER PRIMARY KEY AUTOINCREMENT, expense_date TEXT NOT NULL, category TEXT NOT NULL, amount REAL NOT NULL, notes TEXT); CREATE INDEX IF NOT EXISTS idx_expenses_date ON expenses(expense_date,id);";
        command.ExecuteNonQuery();
    }
}
