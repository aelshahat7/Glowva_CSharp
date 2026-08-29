using GlowvaERP.Services;

namespace GlowvaERP.Forms;

public sealed class LegacyImportForm : Form
{
    private readonly TextBox _sourcePath = new();
    private readonly Label _status = new();
    private readonly LegacyImportService _service = new();

    public LegacyImportForm()
    {
        Text = "استيراد البيانات القديمة";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 360);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false;
        BackColor = Color.FromArgb(248, 248, 248);
        BuildUi();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Text = "استيراد قاعدة بيانات Glowva القديمة",
            Dock = DockStyle.Top,
            Height = 70,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes,
            Padding = new Padding(20, 0, 20, 0)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RightToLeft = RightToLeft.Yes
        };

        var pathLabel = new Label
        {
            Text = "ملف قاعدة البيانات القديمة (.db):",
            AutoSize = true,
            Location = new Point(520, 30),
            RightToLeft = RightToLeft.Yes
        };

        _sourcePath.Location = new Point(120, 24);
        _sourcePath.Width = 380;
        _sourcePath.TextAlign = HorizontalAlignment.Right;
        _sourcePath.RightToLeft = RightToLeft.Yes;

        var browse = new Button
        {
            Text = "اختيار الملف",
            Location = new Point(20, 21),
            Width = 90,
            Height = 32,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        browse.Click += (_, _) => Browse();

        var warning = new Label
        {
            Text = "سيتم أخذ نسخة احتياطية من قاعدة البيانات الحالية قبل الاستيراد، ثم استبدال بياناتها ببيانات القاعدة القديمة.",
            AutoSize = false,
            Width = 620,
            Height = 52,
            Location = new Point(20, 82),
            ForeColor = Color.DarkRed,
            TextAlign = ContentAlignment.MiddleRight,
            RightToLeft = RightToLeft.Yes
        };

        var import = new Button
        {
            Text = "استيراد البيانات",
            Location = new Point(370, 155),
            Width = 220,
            Height = 45,
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        import.Click += (_, _) => Import();

        var cancel = new Button
        {
            Text = "إغلاق",
            Location = new Point(260, 155),
            Width = 90,
            Height = 45,
            FlatStyle = FlatStyle.Flat
        };
        cancel.Click += (_, _) => Close();

        _status.AutoSize = false;
        _status.Width = 620;
        _status.Height = 60;
        _status.Location = new Point(20, 215);
        _status.TextAlign = ContentAlignment.MiddleRight;
        _status.RightToLeft = RightToLeft.Yes;

        panel.Controls.Add(pathLabel);
        panel.Controls.Add(_sourcePath);
        panel.Controls.Add(browse);
        panel.Controls.Add(warning);
        panel.Controls.Add(import);
        panel.Controls.Add(cancel);
        panel.Controls.Add(_status);

        Controls.Add(panel);
        Controls.Add(title);
    }

    private void Browse()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "اختيار قاعدة البيانات القديمة",
            Filter = "SQLite Database (*.db;*.sqlite)|*.db;*.sqlite|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _sourcePath.Text = dialog.FileName;
    }

    private void Import()
    {
        var path = _sourcePath.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "اختر ملف قاعدة البيانات القديمة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "الاستيراد سيستبدل بيانات قاعدة Glowva الحالية بعد أخذ نسخة احتياطية. هل تريد المتابعة؟",
            "تأكيد الاستيراد",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            Cursor = Cursors.WaitCursor;
            _status.Text = "جاري استيراد البيانات...";
            var backup = _service.Import(path);
            _status.Text = $"تم الاستيراد بنجاح.\nالنسخة الاحتياطية: {backup}";
            MessageBox.Show(this, "تم استيراد البيانات القديمة بنجاح. أغلق البرنامج وافتحه مرة أخرى لعرض الإحصاءات الجديدة.", "تم الاستيراد", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _status.Text = "فشل الاستيراد.";
            MessageBox.Show(this, $"تعذر استيراد البيانات:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
