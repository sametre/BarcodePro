namespace BarcodePrinter;

internal sealed class ProductEditor : BarcodePrinter.Controls.Common.AppDialog
{
    private readonly Dictionary<string, TextBox> texts = [];
    private readonly Dictionary<string, NumericUpDown> numbers = [];
    public ProductEditor(Product? existing, Inventory inventory)
    {
        var p = existing?.Copy() ?? new Product();
        Text = existing == null ? "Yeni ürün" : "Ürün bilgileri"; Size = new Size(740, 790); MinimumSize = new Size(640, 650); StartPosition = FormStartPosition.CenterParent; BackColor = Form1.Canvas; Font = new Font("Segoe UI", 10); Padding = new Padding(24); MinimizeBox = false; MaximizeBox = false;
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(0,0,12,0)};
        var fields=new BarcodePrinter.Controls.Common.AppFieldGrid(190);scroll.Controls.Add(fields);
        void Row(string name,Control control)=>fields.AddField(name,control,control.Height>40?control.Height+12:40);        void TextField(string name, string value, bool multiline = false) { var t = new TextBox { Text = value, Multiline = multiline, Height = multiline ? 65 : 28 }; texts[name] = t; Row(name, t); }
        void Number(string name, decimal value) { var n = new NumericUpDown { Maximum = 1000000000, DecimalPlaces = 3, Value = value, ThousandsSeparator = true }; numbers[name] = n; Row(name, n); }
        TextField("Ürün adı *", p.Name); TextField("Barkod *", p.Barcode); TextField("SKU *", p.Sku); TextField("Kategori", p.Category);
        Number("Alış fiyatı (₺)", p.Cost); Number("Satış fiyatı (₺)", p.Price); Number("Eski fiyat (₺, 0=yok)", p.OldPrice ?? 0); Number(existing == null ? "Başlangıç stoğu" : "Mevcut stok", p.Stock);
        if (existing != null) numbers["Mevcut stok"].Enabled = false;
        Number("Minimum stok", p.Minimum); Number("Maksimum stok", p.Maximum);
        TextField("Birim", p.Unit); TextField("Açıklama", p.Description, true);
        var imagePanel = new FlowLayoutPanel { Height = 88, FlowDirection = FlowDirection.LeftToRight };
        var preview = new PictureBox { Width = 80, Height = 80, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
        void Preview() { preview.Image?.Dispose(); preview.Image = null; if (File.Exists(p.ImagePath)) { try { using var original = Image.FromFile(p.ImagePath); preview.Image = new Bitmap(original); } catch (ArgumentException) { } } }
        var imagePath = new Label { Text = string.IsNullOrEmpty(p.ImagePath) ? "Görsel seçilmedi" : Path.GetFileName(p.ImagePath), AutoSize = true, MaximumSize = new Size(150, 60) };
        imagePanel.ControlAdded += (_, e) => { if(e.Control is Button) e.Control.Margin = new Padding(8, 26, 12, 0); if(e.Control is Label) e.Control.Margin = new Padding(0, 30, 0, 0); };
        imagePanel.Controls.Add(preview); imagePanel.Controls.Add(Form1.Button("Görsel seç", () => { using var picker = new OpenFileDialog { Filter = "Görsel|*.jpg;*.jpeg;*.png;*.bmp" }; if (picker.ShowDialog(this) == DialogResult.OK) { try { using var check = Image.FromFile(picker.FileName); p.ImagePath = picker.FileName; imagePath.Text = Path.GetFileName(p.ImagePath); Preview(); } catch (Exception ex) when (ex is ArgumentException or IOException or OutOfMemoryException) { MessageBox.Show(this, "Görsel okunamadı."); } } }, false)); imagePanel.Controls.Add(imagePath); Row("Ürün görseli", imagePanel); Preview();
        var menu = new CheckBox { Text = "Menüde göster", Checked = p.OnMenu }; var active = new CheckBox { Text = "Ürün aktif", Checked = p.Active }; Row("Menü durumu", menu); Row("Ürün durumu", active);
        if (existing != null) Row("Kayıt tarihleri", new Label { Text = $"Oluşturulma: {p.CreatedAt:dd.MM.yyyy HH:mm}\nGüncelleme: {p.UpdatedAt:dd.MM.yyyy HH:mm}", Height = 48 });
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(0, 16, 0, 0), FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(Form1.Button("Ürünü kaydet", () =>
        {
            try
            {
                p.Name = texts["Ürün adı *"].Text; p.Barcode = texts["Barkod *"].Text; p.Sku = texts["SKU *"].Text; p.Category = texts["Kategori"].Text.Trim(); p.Unit = texts["Birim"].Text.Trim(); p.Description = texts["Açıklama"].Text;
                p.Cost = numbers["Alış fiyatı (₺)"].Value; p.Price = numbers["Satış fiyatı (₺)"].Value; p.OldPrice = numbers["Eski fiyat (₺, 0=yok)"].Value == 0 ? null : numbers["Eski fiyat (₺, 0=yok)"].Value; p.Stock = numbers[existing == null ? "Başlangıç stoğu" : "Mevcut stok"].Value; p.Minimum = numbers["Minimum stok"].Value; p.Maximum = numbers["Maksimum stok"].Value; p.OnMenu = menu.Checked; p.Active = active.Checked;
                if (p.Unit.Length == 0 || p.Category.Length == 0) throw new InvalidOperationException("Birim ve kategori boş bırakılamaz.");
                if (File.Exists(p.ImagePath))
                {
                    var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarcodePro", "images"); Directory.CreateDirectory(folder);
                    var target = Path.Combine(folder, p.Id + Path.GetExtension(p.ImagePath));
                    if (!Path.GetFullPath(p.ImagePath).Equals(target, StringComparison.OrdinalIgnoreCase)) File.Copy(p.ImagePath, target, true);
                    p.ImagePath = target;
                }
                inventory.SaveProduct(p); DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException) { MessageBox.Show(this, ex.Message, "Ürün kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }));
        buttons.Controls.Add(Form1.Button("Vazgeç", () => DialogResult = DialogResult.Cancel, false));
        Controls.Add(scroll); Controls.Add(buttons); FormClosed += (_, _) => preview.Image?.Dispose();
    }
}



