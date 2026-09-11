using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Licensing;

namespace BarcodePrinter;

public sealed class LicenseActivationForm : AppWindow
{
    public LicenseActivationForm(bool server)
    {
        Text = "R3 M-Kobi · License activation"; ClientSize = new Size(390, 300); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26, 18, 26, 20), ColumnCount = 1, RowCount = 7 };
        foreach (var h in new[] { 42, 30, 28, 34, 38, 36, 24 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        var title = new Label { Text = "R3 M-Kobi license", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        var info = new Label { Text = $"{(server ? "Server" : "Client")} requires a valid license file.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Themes.ThemeManager.Current.Muted };
        var key = new AppTextBox { PlaceholderText = "6-character license number", Dock = DockStyle.Fill, CharacterCasing = CharacterCasing.Upper, MaxLength = 6 };
        var file = new AppTextBox { ReadOnly = true, Dock = DockStyle.Fill };
        var browse = new AppButton { Text = "License file", IconKind = AppIcon.Template, Dock = DockStyle.Fill };
        var activate = new AppButton { Text = "Activate", IconKind = AppIcon.Save, Tag = "primary", Dock = DockStyle.Top, Height = 30 };
        var status = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Firebrick };
        browse.Click += (_, _) => { using var dialog = new OpenFileDialog { Filter = "R3 license|license.json|JSON|*.json", Title = "Select R3 license file" }; if (dialog.ShowDialog(this) == DialogResult.OK) file.Text = dialog.FileName; };
        activate.Click += (_, _) => { if (string.IsNullOrWhiteSpace(file.Text)) { status.Text = "Select license.json first."; return; } if (LicenseService.Activate(file.Text, key.Text, server, out var message)) { DialogResult = DialogResult.OK; Close(); } else status.Text = message; };
        var fileRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fileRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112)); fileRow.Controls.Add(file, 0, 0); fileRow.Controls.Add(browse, 1, 0);
        layout.Controls.Add(new PictureBox { Image = Themes.BrandAssets.CreateMark(38), SizeMode = PictureBoxSizeMode.CenterImage, Dock = DockStyle.Fill }, 0, 0); layout.Controls.Add(title, 0, 1); layout.Controls.Add(info, 0, 2); layout.Controls.Add(key, 0, 3); layout.Controls.Add(fileRow, 0, 4); layout.Controls.Add(activate, 0, 5); layout.Controls.Add(status, 0, 6); Controls.Add(layout);
        AcceptButton = activate; Shown += (_, _) => { Themes.ThemeManager.Apply(this); key.Focus(); };
    }
}
