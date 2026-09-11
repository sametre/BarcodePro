using BarcodePrinter.Controls.Common;
using BarcodePrinter.Themes;

namespace BarcodePrinter;

internal sealed class ProductEditor : AppDialog
{
    private readonly Dictionary<string, TextBox> texts = [];
    private readonly Dictionary<string, NumericUpDown> numbers = [];
    public ProductEditor(Product? existing, Inventory inventory)
    {
        var p = existing?.Copy() ?? new Product();
        Text = existing == null ? "Yeni ürün" : "Ürün bilgileri · " + p.Name;
        Size = new Size(780, 560); MinimumSize = new Size(660, 530); Padding = new Padding(10); MinimizeBox = false; MaximizeBox = false; Font = AppTypography.Body();
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 5) };
        AppFieldGrid Section(string caption)
        {
            var tab = new TabPage(caption) { Padding = new Padding(12), AutoScroll = true };
            var fields = new AppFieldGrid(160); tab.Controls.Add(fields); tabs.TabPages.Add(tab); return fields;
        }
        void TextField(AppFieldGrid fields, string name, string value, bool multiline = false)
        {
            var text = new AppTextBox { Text = value, Multiline = multiline, Height = multiline ? 82 : 25 };
            texts[name] = text; fields.AddField(name, text, multiline ? 90 : 34);
        }
        void Number(AppFieldGrid fields, string name, decimal value)
        {
            var number = new AppNumericInput { Maximum = 1000000000, DecimalPlaces = 3, Value = Math.Clamp(value, 0, 1000000000), ThousandsSeparator = true };
            numbers[name] = number; fields.AddField(name, number);
        }
        var main = Section("Ürün bilgileri");
        TextField(main, "Ürün adı *", p.Name); TextField(main, "Barkod *", p.Barcode); TextField(main, "SKU *", p.Sku);
        TextField(main, "Kategori", p.Category); TextField(main, "Birim", p.Unit);
        var menu = new AppToggle { Text = "Menüde göster", Checked = p.OnMenu };
        var active = new AppToggle { Text = "Ürün aktif", Checked = p.Active };
        main.AddField("Menü durumu", menu); main.AddField("Ürün durumu", active);
        var prices = Section("Fiyat ve stok");
        Number(prices, "Alış fiyatı (₺)", p.Cost); Number(prices, "Satış fiyatı (₺)", p.Price); Number(prices, "Eski fiyat (₺, 0=yok)", p.OldPrice ?? 0);
        string stockCaption = existing == null ? "Başlangıç stoğu" : "Mevcut stok";
        Number(prices, stockCaption, p.Stock); if (existing != null) numbers[stockCaption].Enabled = false;
        Number(prices, "Minimum stok", p.Minimum); Number(prices, "Maksimum stok", p.Maximum);
        if (existing != null) prices.AddField("Stok işlemi", new Label { Text = "Stok değişikliklerini Stok menüsünden yapın.", TextAlign = ContentAlignment.MiddleLeft }, 34);
        var details = Section("Görsel ve açıklama");
        TextField(details, "Açıklama", p.Description, true);
        var preview = new PictureBox { Size = new Size(132, 132), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        var imageState = new Label { AutoSize = false, Height = 48, Width = 260, AutoEllipsis = true };
        void Preview()
        {
            preview.Image?.Dispose(); preview.Image = null;
            try
            {
                if (p.ImageData is { Length: > 0 }) { using var stream = new MemoryStream(p.ImageData); using var original = Image.FromStream(stream); preview.Image = new Bitmap(original); }
                else if (File.Exists(p.ImagePath)) { using var original = Image.FromFile(p.ImagePath); preview.Image = new Bitmap(original); }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException) { }
            imageState.Text = preview.Image != null ? "Görsel ürünle birlikte Server'da saklanır." : string.IsNullOrWhiteSpace(p.SourceImage) ? "Ürün görseli seçilmedi." : "Kaynak görsel bekleniyor: " + p.SourceImage;
        }
        var imageActions = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 275, Height = 132, Margin = new Padding(10, 0, 0, 0) };
        var choose = Form1.Button("Görsel seç", () =>
        {
            using var picker = new OpenFileDialog { Filter = "Görsel|*.jpg;*.jpeg;*.png;*.bmp" };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                using var original = Image.FromFile(picker.FileName);
                var ratio = Math.Min(1d, 1000d / Math.Max(original.Width, original.Height));
                using var bitmap = new Bitmap(Math.Max(1, (int)(original.Width * ratio)), Math.Max(1, (int)(original.Height * ratio)));
                using (var graphics = Graphics.FromImage(bitmap)) { graphics.Clear(Color.White); graphics.DrawImage(original, 0, 0, bitmap.Width, bitmap.Height); }
                using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                p.ImageData = stream.ToArray(); p.SourceImage = Path.GetFileName(picker.FileName); p.ImagePath = ""; Preview();
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException) { MessageBox.Show(this, "Görsel okunamadı."); }
        }, false);
        var remove = Form1.Button("Görseli kaldır", () => { p.ImageData = null; p.ImagePath = ""; p.SourceImage = ""; Preview(); }, false);
        imageActions.Controls.AddRange([choose, remove, imageState]);
        var imageRow = new FlowLayoutPanel { Height = 140, WrapContents = false }; imageRow.Controls.AddRange([preview, imageActions]); details.AddField("Ürün görseli", imageRow, 145); Preview();
        if (p.SourceFields.Count > 0)
        {
            var source = new TabPage("Kaynak tablo alanları") { Padding = new Padding(8) };
            var grid = new AppDataGrid { MultiSelect = false };
            grid.DataSource = p.SourceFields.Select(pair => new { Alan = pair.Key, KaynakDeğer = pair.Value }).ToList();
            source.Controls.Add(grid); source.Controls.Add(new Label { Dock = DockStyle.Top, Height = 32, Text = "İçe aktarılan orijinal tablo alanları ürün kaydında korunur.", TextAlign = ContentAlignment.MiddleLeft }); tabs.TabPages.Add(source);
        }
        var actions = new AppToolbar { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 6, 0, 0) };
        actions.Controls.Add(Form1.Button("Ürünü kaydet", () =>
        {
            try
            {
                p.Name = texts["Ürün adı *"].Text; p.Barcode = texts["Barkod *"].Text; p.Sku = texts["SKU *"].Text; p.Category = texts["Kategori"].Text.Trim(); p.Unit = texts["Birim"].Text.Trim(); p.Description = texts["Açıklama"].Text;
                p.Cost = numbers["Alış fiyatı (₺)"].Value; p.Price = numbers["Satış fiyatı (₺)"].Value; p.OldPrice = numbers["Eski fiyat (₺, 0=yok)"].Value == 0 ? null : numbers["Eski fiyat (₺, 0=yok)"].Value;
                p.Stock = numbers[stockCaption].Value; p.Minimum = numbers["Minimum stok"].Value; p.Maximum = numbers["Maksimum stok"].Value; p.OnMenu = menu.Checked; p.Active = active.Checked;
                if (p.Unit.Length == 0 || p.Category.Length == 0) throw new InvalidOperationException("Birim ve kategori boş bırakılamaz.");
                inventory.SaveProduct(p); DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
            { MessageBox.Show(this, ex.Message, "Ürün kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }));
        actions.Controls.Add(Form1.Button("Vazgeç", () => DialogResult = DialogResult.Cancel, false));
        var dates = new Label { Dock = DockStyle.Bottom, Height = 25, Font = AppTypography.Small(), TextAlign = ContentAlignment.MiddleLeft, Text = existing == null ? "* Zorunlu alanlar" : $"Oluşturulma: {p.CreatedAt:dd.MM.yyyy HH:mm}   ·   Güncelleme: {p.UpdatedAt:dd.MM.yyyy HH:mm}" };
        Controls.Add(tabs); Controls.Add(dates); Controls.Add(actions); FormClosed += (_, _) => preview.Image?.Dispose();
    }
}
