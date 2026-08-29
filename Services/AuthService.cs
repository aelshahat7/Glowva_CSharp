using System.Security.Cryptography;
using System.Text;
using GlowvaERP.Data;

namespace GlowvaERP.Services;

public sealed record AuthUser(long Id, string Username, string DisplayName, bool IsAdmin, string Permissions);

public static class AuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 120_000;
    public static AuthUser? CurrentUser { get; private set; }

    public static bool Login(string username, string password)
    {
        using var c = Database.OpenConnection();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id,username,display_name,password_hash,is_admin,permissions FROM users WHERE username=$u AND is_active=1 LIMIT 1;";
        cmd.Parameters.AddWithValue("$u", username.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read() || !Verify(password, r.GetString(3))) return false;
        CurrentUser = new AuthUser(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt32(4) == 1, r.IsDBNull(5) ? "" : r.GetString(5));
        return true;
    }

    public static void Logout() => CurrentUser = null;

    public static bool Can(string permission)
    {
        var u = CurrentUser;
        return u != null && (u.IsAdmin || u.Permissions == "*" || u.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(permission, StringComparer.OrdinalIgnoreCase));
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool Verify(string password, string encoded)
    {
        try
        {
            var p = encoded.Split('$');
            if (p.Length == 4 && p[0] == "PBKDF2-SHA256")
            {
                var iterations = int.Parse(p[1]);
                var salt = Convert.FromBase64String(p[2]);
                var expected = Convert.FromBase64String(p[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            using var sha = SHA256.Create();
            var legacy = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return string.Equals(legacy, encoded, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
