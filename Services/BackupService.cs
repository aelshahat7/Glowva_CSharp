using GlowvaERP.Data;

namespace GlowvaERP.Services;

public static class BackupService
{
    public static string CreateBackup(string? destinationDirectory = null)
    {
        var directory = destinationDirectory;
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GlowvaERP", "Backups");
        }
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"glowva_erp_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        File.Copy(Database.DatabasePath, path, overwrite: false);
        return path;
    }

    public static void Restore(string backupPath)
    {
        if (!File.Exists(backupPath)) throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود.", backupPath);
        if (string.Equals(Path.GetFullPath(backupPath), Path.GetFullPath(Database.DatabasePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("لا يمكن استعادة قاعدة البيانات من الملف المفتوح نفسه.");
        File.Copy(backupPath, Database.DatabasePath, overwrite: true);
    }
}
