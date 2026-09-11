using BarcodePrinter.Controls.Common;
using BarcodePrinter.Helpers;
using BarcodePrinter.Themes;

namespace BarcodePrinter;

/// <summary>Preview a MySQL export and commit the mapped products to the server inventory.</summary>
public sealed class SqlFileImportPanel : UserControl
{
    private readonly Inventory inventory;
    private readonly AppTextBox file = new() { ReadOnly = true };
    private readonly AppTextBox imageFolder = new() { PlaceholderText = "Görsellerin bulunduğu klasör (isteğe bağlı)" };
    private readonly AppTextBox imageUrl = new() { PlaceholderText = "https://firmaniz.com/storage/ (isteğe bağlı)" };
    private readonly AppDataGrid preview = new();
    private readonly Label summary = new() { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8), Text = "SQL dosyasını seçin. Ürünler önce önizlenir, ardından Server veritabanına aktarılır." };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(8), AutoEllipsis = true };
    private readonly CheckBox updateStock = new() { Text = "Eşleşen ürünlerin stoklarını da güncelle", AutoSize = true };
    private readonly AppButton read = new() { Text = "Önizle", IconKind = AppIcon.Products };
    private readonly AppButton images = new() { Text = "Görselleri yükle", IconKind = AppIcon.Export, Enabled = false };
    private readonly AppButton import = new() { Text = "SQLite'a aktar", IconKind = AppIcon.Add, Enabled = false };
    private readonly AppButton cancel = new() { Text = "İptal", IconKind = AppIcon.Close, Enabled = false };
    private readonly List<Control> selectors = [];
    private ProductSqlImportResult? result;
    private CancellationTokenSource? operation;

    public SqlFileImportPanel(Inventory inventory)
    {
        this.inventory = inventory;
        Dock = DockStyle.Fill; Padding = new Padding(10); Font = AppTypography.Body();
        var select = new AppButton { Text = "SQL seç", IconKind = AppIcon.Template };
        select.Click += (_, _) =>
        {
            using var picker = new OpenFileDialog { Filter = "MySQL ürün dökümü|*.sql", Title = "Ürün SQL dosyasını seçin" };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            file.Text = picker.FileName; result = null; preview.DataSource = null; SetBusy(false);
            status.Text = "Dosya seçildi. Önizle düğmesi ile ürünleri ve görsel durumlarını kontrol edin.";
        };
        var folder = new AppButton { Text = "Klasör seç", IconKind = AppIcon.Template }; selectors.AddRange([select, folder]);
        folder.Click += (_, _) => { using var picker = new FolderBrowserDialog { Description = "products klasörünü veya üst görsel klasörünü seçin" }; if (picker.ShowDialog(this) == DialogResult.OK) imageFolder.Text = picker.SelectedPath; };
        var form = new AppFieldGrid(130);
        Control WithButton(Control input, Control button)
        {
            var row = new TableLayoutPanel { ColumnCount = 2, Margin = Padding.Empty, Height = 27 };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            input.Dock = DockStyle.Fill; input.Margin = new Padding(0, 1, 6, 0); button.Dock = DockStyle.Fill; button.Margin = Padding.Empty;
            row.Controls.Add(input); row.Controls.Add(button); return row;
        }
        form.AddField("Ürün SQL dosyası", WithButton(file, select), 35);
        form.AddField("Görsel klasörü", WithButton(imageFolder, folder), 35);
        form.AddField("Görsel temel adresi", imageUrl, 35);
        var toolbar = new AppToolbar(); toolbar.Controls.AddRange([read, images, import, cancel, updateStock]);
        var help = new Label
        {
            Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(8, 5, 8, 2),
            Text = "Görseller SQL içinde dosya yolu olarak bulunabilir. Görsel klasörünü veya sitenizin temel adresini girin.\nBarkod / SKU eşleşmeleri güncellenir; yeni ürünler eklenir. Kaynak tablodaki ek alanlar ürün kaydında korunur."
        };
        preview.AutoGenerateColumns = true;
        Controls.Add(preview); Controls.Add(summary); Controls.Add(toolbar); Controls.Add(form); Controls.Add(help); Controls.Add(status);
        read.Click += (_, _) => Run(async token =>
        {
            var source = file.Text;
            if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException("Önce ürün SQL dosyasını seçin.");
            result = await Task.Run(() => ProductSqlImport.ReadFile(source), token);
            token.ThrowIfCancellationRequested();
            await LoadImages(token); ShowPreview();
            status.Text = result.Warnings.Count==0?"Önizleme hazır. SQLite'a aktar düğmesi ile ürünleri kaydedebilirsiniz.":$"Önizleme hazır · {result.Warnings.Count:N0} eşleştirme notu: "+string.Join(" ",result.Warnings.Take(3));
        });
        images.Click += (_, _) => Run(async token => { await LoadImages(token); ShowPreview(); status.Text = "Görsel kontrolü tamamlandı. Görsel bulunamayan ürünlerin kaynak yolları korunur."; });
        import.Click += (_, _) => Run(async token =>
        {
            token.ThrowIfCancellationRequested();
            var source = result ?? throw new InvalidOperationException("Önce dosyayı önizleyin.");
            var count = inventory.ImportProducts(source.Products, source.MappedFields.ToArray(), updateStock.Checked);
            status.Text = $"SQLite aktarımı tamamlandı: {count.Added:N0} yeni ürün, {count.Updated:N0} güncellenen ürün. {source.ImagesLoaded:N0} ürün görseli kayıtlı.";
            result = null; await Task.CompletedTask;
        });
        cancel.Click += (_, _) => operation?.Cancel();
        Disposed += (_, _) => operation?.Cancel();
        ThemeManager.Apply(this);
    }

    private async Task LoadImages(CancellationToken token)
    {
        if (result == null) throw new InvalidOperationException("Önce SQL dosyasını önizleyin.");
        Uri? baseUri = null;
        if (!string.IsNullOrWhiteSpace(imageUrl.Text) && (!Uri.TryCreate(imageUrl.Text.Trim(), UriKind.Absolute, out baseUri) || baseUri.Scheme is not ("http" or "https")))
            throw new InvalidOperationException("Görsel adresi http:// veya https:// ile başlamalıdır.");
        await ProductSqlImport.LoadImagesAsync(result, string.IsNullOrWhiteSpace(imageFolder.Text) ? null : imageFolder.Text.Trim(), baseUri, token);
    }

    private void ShowPreview()
    {
        if (result == null || IsDisposed) return;
        preview.DataSource = result.Products.Select(p => new
        {
            Ürün = p.Name, Barkod = p.Barcode, SKU = p.Sku, Kategori = p.Category, Fiyat = p.Price, Stok = p.Stock,
            Görsel = p.ImageData is { Length: > 0 } ? "Hazır" : string.IsNullOrWhiteSpace(p.SourceImage) ? "Kaynakta yok" : "Dosya bekleniyor",
            KaynakGörsel = p.SourceImage, EkAlan = p.SourceFields.Count
        }).ToList();
        summary.Text = $"{result.Products.Count:N0} ürün  •  {result.ImagesLoaded:N0} görsel hazır  •  {result.ImagesMissing:N0} görsel dosyası bekleniyor  •  {result.ImagesUnspecified:N0} üründe kaynak görsel yok";
    }

    private async void Run(Func<CancellationToken, Task> action)
    {
        if (operation != null) return;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10)); operation = cancellation; SetBusy(true); status.Text = "İşlem sürüyor…";
        try { await action(cancellation.Token); }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = "İşlem iptal edildi veya süre doldu."; }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException or UnauthorizedAccessException or System.Text.Json.JsonException or Microsoft.Data.Sqlite.SqliteException or HttpRequestException)
        { JsonStore.Log(ex); if (!IsDisposed) status.Text = "İşlem tamamlanamadı: " + ex.Message; }
        finally { operation = null; if (!IsDisposed) SetBusy(false); }
    }
    private void SetBusy(bool busy)
    {
        read.Enabled = !busy; images.Enabled = import.Enabled = !busy && result is { Products.Count: > 0 }; cancel.Enabled = busy;
        file.Enabled = imageFolder.Enabled = imageUrl.Enabled = updateStock.Enabled = !busy;
        foreach(var selector in selectors) selector.Enabled = !busy;
    }
}
