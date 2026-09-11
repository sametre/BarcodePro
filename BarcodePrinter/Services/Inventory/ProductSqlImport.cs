using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace BarcodePrinter;

public sealed class ProductSqlImportResult
{
    public List<Product> Products { get; } = [];
    public List<string> Warnings { get; } = [];
    public IReadOnlyList<string> MappedFields { get; } = [nameof(Product.Name), nameof(Product.Barcode), nameof(Product.Sku), nameof(Product.Category), nameof(Product.Cost), nameof(Product.Price), nameof(Product.Stock), nameof(Product.Minimum), nameof(Product.Maximum), nameof(Product.Unit), nameof(Product.Description), nameof(Product.SourceImage), nameof(Product.ImageData), nameof(Product.SourceFields), nameof(Product.Active), nameof(Product.OnMenu), nameof(Product.CreatedAt), nameof(Product.UpdatedAt)];
    public int Rows => Products.Count;
    public int ImagesReferenced => Products.Count(p => !string.IsNullOrWhiteSpace(p.SourceImage));
    public int ImagesLoaded => Products.Count(p => p.ImageData is { Length: > 0 });
    public int ImagesMissing => Products.Count(p => !string.IsNullOrWhiteSpace(p.SourceImage) && p.ImageData is not { Length: > 0 });
    public int ImagesUnspecified => Products.Count(p => string.IsNullOrWhiteSpace(p.SourceImage));
}

/// <summary>Reads SQL literals as data. No source statement is ever executed against a database.</summary>
public static class ProductSqlImport
{
    public const int MaximumRows = 50000;
    public const int MaximumSqlBytes = 64 * 1024 * 1024;
    public const int MaximumImageBytes = 8 * 1024 * 1024;

    public static ProductSqlImportResult ReadFile(string path)
    {
        if (new FileInfo(path).Length > MaximumSqlBytes) throw new InvalidOperationException("SQL dosyası 64 MB sınırını aşıyor.");
        return Parse(File.ReadAllText(path, Encoding.UTF8), Path.GetFileName(path));
    }

    public static ProductSqlImportResult Parse(string sql, string sourceName = "products.sql")
    {
        if (sql.Length > MaximumSqlBytes) throw new InvalidOperationException("SQL dosyası çok büyük.");
        var parser = new LiteralReader(sql);
        var result = new ProductSqlImportResult();
        var barcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!parser.AtEnd)
        {
            parser.Keyword("INSERT");
            parser.Keyword("INTO");
            string table = parser.Identifier();
            if (parser.TryTake('.')) table += "." + parser.Identifier();
            if (!table.Split('.').Last().Equals("products", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Yalnızca products tablosunun INSERT kayıtları aktarılabilir.");
            parser.Take('(');
            var columns = new List<string>();
            do { columns.Add(parser.Identifier()); } while (parser.TryTake(','));
            parser.Take(')');
            if (columns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != columns.Count) throw new InvalidOperationException("SQL içinde tekrar eden kolon var.");
            if (!columns.Contains("name", StringComparer.OrdinalIgnoreCase)) throw new InvalidOperationException("SQL içinde name kolonu zorunludur.");
            parser.Keyword("VALUES");
            do
            {
                parser.Take('(');
                var values = new List<string?>();
                do { values.Add(parser.Value()); } while (parser.TryTake(','));
                parser.Take(')');
                if (values.Count != columns.Count) throw new InvalidOperationException($"{result.Rows + 1}. kayıtta kolon sayısı eşleşmiyor.");
                if (result.Rows >= MaximumRows) throw new InvalidOperationException("Tek aktarımda en fazla 50.000 ürün desteklenir.");
                var fields = columns.Zip(values).ToDictionary(x => x.First, x => x.Second, StringComparer.OrdinalIgnoreCase);
                var product = Map(fields, table, sourceName, result.Rows + 1, result.Warnings);
                if (!barcodes.Add(product.Barcode)) throw new InvalidOperationException($"{result.Rows + 1}. kaydın barkodu başka bir kaynak üründe de var: {product.Barcode}. Hiçbir kayıt aktarılmadı.");
                if (!skus.Add(product.Sku))
                {
                    string original = product.Sku;
                    product.Sku += " [" + product.Id.ToString("N")[..12] + "]";
                    if (!skus.Add(product.Sku)) throw new InvalidOperationException("Kaynakta ürün kimliği yineleniyor. Hiçbir kayıt aktarılmadı.");
                    result.Warnings.Add($"{result.Rows + 1}. kaydın tekrar eden SKU'suna ayırt edici ek kondu; özgün kod kaynak alanlarında korundu: {original}");
                }
                result.Products.Add(product);
            } while (parser.TryTake(','));
            parser.Take(';');
        }
        if (result.Rows == 0) throw new InvalidOperationException("SQL dosyasında ürün bulunamadı.");
        return result;
    }

    private static Product Map(Dictionary<string, string?> fields, string table, string sourceName, int row, List<string> warnings)
    {
        string Text(string key, string fallback = "") => fields.GetValueOrDefault(key) ?? fallback;
        decimal Number(string key, decimal fallback = 0)
        {
            string value = Text(key);
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            if (!decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal number)) throw new InvalidOperationException($"{row}. kaydın {key} değeri geçersiz.");
            return number;
        }
        DateTime Date(string key, DateTime fallback)
        {
            string value = Text(key);
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : fallback;
        }
        string name = Text("name").Trim(), originalBarcode = Text("barcode").Trim(), originalCode = Text("code", Text("sku")).Trim();
        if (name.Length == 0) throw new InvalidOperationException($"{row}. kaydın ürün adı boş.");
        string identity = Text("id");
        if (string.IsNullOrWhiteSpace(identity)) identity = originalBarcode.Length > 0 ? "barcode:" + originalBarcode : "code:" + originalCode;
        if (identity == "code:") identity = "row:" + row.ToString(CultureInfo.InvariantCulture) + ":" + name;
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(table.ToLowerInvariant() + ":" + identity));
        var id = new Guid(digest.AsSpan(0, 16));
        string barcode = originalBarcode;
        if (!Inventory.ValidBarcode(barcode))
        {
            barcode = "BP" + Convert.ToHexString(digest.AsSpan(0, 10));
            warnings.Add($"{row}. kaydın barkodu {(string.IsNullOrWhiteSpace(originalBarcode) ? "boştu" : "desteklenen biçimde değildi")}; {barcode} yerel barkodu üretildi. Özgün barkod kaynak alanlarında korundu.");
        }
        decimal minimum = Number("min_stock"), maximum = Math.Max(minimum, Number("max_stock", 100));
        bool active = Text("is_active", "1") is "1" or "true" or "TRUE";
        bool deleted = !string.IsNullOrWhiteSpace(Text("deleted_at"));
        DateTime created = Date("created_at", DateTime.UnixEpoch), updated = Date("updated_at", created);
        var product = new Product
        {
            Id = id, Name = name, Barcode = barcode, Sku = originalCode.Length > 0 ? originalCode : "SQL-" + id.ToString("N"),
            Category = Text("category", Text("category_name", Text("category_id").Length > 0 ? "Kategori #" + Text("category_id") : "Genel")),
            Unit = Text("unit", Text("unit_name", Text("unit_id").Length > 0 ? "Birim #" + Text("unit_id") : "Adet")),
            Cost = Number("purchase_price"), Price = Number("sale_price"), Stock = Number("current_stock"), Minimum = minimum, Maximum = maximum,
            Description = Text("description"), SourceImage = Text("image"), SourceFields = fields,
            Active = active && !deleted, OnMenu = active && !deleted, CreatedAt = created, UpdatedAt = updated
        };
        if (product.Cost < 0 || product.Price < 0 || product.Stock < 0 || product.Minimum < 0) throw new InvalidOperationException($"{row}. kayıtta negatif fiyat veya stok var.");
        fields["_source_table"] = table; fields["_source_file"] = sourceName; fields["_source_row"] = row.ToString(CultureInfo.InvariantCulture);
        return product;
    }

    public static async Task LoadImagesAsync(ProductSqlImportResult result, string? imagesFolder = null, Uri? imageBaseUri = null, CancellationToken cancellationToken = default)
    {
        if (imageBaseUri != null && (!imageBaseUri.IsAbsoluteUri || imageBaseUri.Scheme is not ("http" or "https") || imageBaseUri.UserInfo.Length > 0)) throw new InvalidOperationException("Görsel adresi http:// veya https:// ile başlamalıdır.");
        string? root = string.IsNullOrWhiteSpace(imagesFolder) ? null : Path.GetFullPath(imagesFolder);
        if (root != null && !Directory.Exists(root)) throw new InvalidOperationException("Seçilen görsel klasörü bulunamadı.");
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
        foreach (var product in result.Products.Where(p => !string.IsNullOrWhiteSpace(p.SourceImage) && p.ImageData is not { Length: > 0 }))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                byte[]? bytes = null;
                string reference = product.SourceImage.Replace('\\', '/');
                if (root != null)
                {
                    string? local = ResolveImageFile(root, reference);
                    if (local != null)
                    {
                        if (new FileInfo(local).Length > MaximumImageBytes) throw new InvalidDataException("Görsel 8 MB sınırını aşıyor.");
                        bytes = await File.ReadAllBytesAsync(local, cancellationToken);
                    }
                }
                if (bytes == null && imageBaseUri != null)
                {
                    Uri imageUri;
                    if (Uri.TryCreate(reference, UriKind.Absolute, out var absolute))
                    {
                        if (absolute.Scheme is not ("http" or "https") || absolute.UserInfo.Length > 0 || !absolute.Authority.Equals(imageBaseUri.Authority, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Görsel adresi seçilen sunucunun dışında.");
                        imageUri = absolute;
                    }
                    else
                    {
                        if (reference.Split('/').Any(p => p is "." or "..") || reference.StartsWith('/')) throw new InvalidDataException("Görsel yolu geçersiz.");
                        imageUri = new Uri(new Uri(imageBaseUri.AbsoluteUri.TrimEnd('/') + "/"), reference);
                    }
                    using var response = await client.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > MaximumImageBytes) throw new InvalidDataException("Görsel 8 MB sınırını aşıyor.");
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var memory = new MemoryStream();
                    var buffer = new byte[81920]; int read;
                    while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        if (memory.Length + read > MaximumImageBytes) throw new InvalidDataException("Görsel 8 MB sınırını aşıyor.");
                        memory.Write(buffer, 0, read);
                    }
                    bytes = memory.ToArray();
                }
                if (bytes != null) product.ImageData = NormalizeImage(bytes);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is IOException or HttpRequestException or ArgumentException or OperationCanceledException)
            { result.Warnings.Add($"{product.Name}: görsel alınamadı ({ex.Message}). Kaynak görsel yolu korundu."); }
        }
    }

    public static string? ResolveImageFile(string root, string reference)
    {
        root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var uri)) reference = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
        reference = reference.Replace('\\', '/');
        if (reference.Split('/').Any(p => p is "." or "..") || Path.IsPathRooted(reference)) throw new InvalidDataException("Görsel yolu seçilen klasörün dışına çıkamaz.");
        foreach (string candidate in new[] { reference, "storage/" + reference, Path.GetFileName(reference) })
        {
            string path = Path.GetFullPath(Path.Combine(root, candidate.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Görsel yolu seçilen klasörün dışına çıkamaz.");
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static byte[] NormalizeImage(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Görsel biçimi okunamadı.");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 40000000) throw new InvalidDataException("Görsel çözünürlüğü çok büyük.");
        using var source = SKBitmap.Decode(codec) ?? throw new InvalidDataException("Görsel bozuk veya desteklenmiyor.");
        double scale = Math.Min(1, 1000d / Math.Max(source.Width, source.Height));
        using var bitmap = new SKBitmap(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(source, new SKRect(0, 0, bitmap.Width, bitmap.Height));
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var output = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        if (output.Size > 2 * 1024 * 1024) throw new InvalidDataException("İşlenen görsel 2 MB sınırını aşıyor.");
        return output.ToArray();
    }

    private sealed class LiteralReader(string text)
    {
        private int position;
        public bool AtEnd { get { White(); return position == text.Length; } }
        private InvalidOperationException Error(string expected) => new($"SQL biçimi desteklenmiyor (karakter {position + 1}, beklenen: {expected}). Yalnızca INSERT VALUES verisi okunur; SQL çalıştırılmaz.");
        private void White()
        {
            while (position < text.Length)
            {
                if (char.IsWhiteSpace(text[position]) || text[position] == '\uFEFF') { position++; continue; }
                if (text[position] == '#' || (text[position] == '-' && position + 1 < text.Length && text[position + 1] == '-')) { while (position < text.Length && text[position] != '\n') position++; continue; }
                if (text[position] == '/' && position + 1 < text.Length && text[position + 1] == '*')
                {
                    int end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                    if (end < 0) throw Error("yorum sonu"); position = end + 2; continue;
                }
                break;
            }
        }
        public bool TryTake(char value) { White(); if (position < text.Length && text[position] == value) { position++; return true; } return false; }
        public void Take(char value) { if (!TryTake(value)) throw Error(value.ToString()); }
        public string Identifier()
        {
            White();
            if (TryTake('`'))
            {
                var builder = new StringBuilder();
                while (position < text.Length)
                {
                    char c = text[position++];
                    if (c == '`') { if (position < text.Length && text[position] == '`') { position++; builder.Append('`'); } else return builder.ToString(); }
                    else builder.Append(c);
                }
                throw Error("kolon adı sonu");
            }
            int start = position;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_')) position++;
            if (start == position) throw Error("kolon/tablo adı");
            return text[start..position];
        }
        public void Keyword(string keyword) { if (!Identifier().Equals(keyword, StringComparison.OrdinalIgnoreCase)) throw Error(keyword); }
        public string? Value()
        {
            White();
            if (TryTake('\''))
            {
                var builder = new StringBuilder();
                while (position < text.Length)
                {
                    char c = text[position++];
                    if (c == '\'') { if (position < text.Length && text[position] == '\'') { position++; builder.Append('\''); } else return builder.ToString(); }
                    else if (c == '\\')
                    {
                        if (position == text.Length) throw Error("kaçış karakteri");
                        char escaped = text[position++];
                        builder.Append(escaped switch { '0' => '\0', 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'Z' => '\x1a', _ => escaped });
                    }
                    else builder.Append(c);
                }
                throw Error("metin sonu");
            }
            int start = position;
            while (position < text.Length && !char.IsWhiteSpace(text[position]) && text[position] is not (',' or ')')) position++;
            string token = text[start..position];
            if (token.Equals("NULL", StringComparison.OrdinalIgnoreCase)) return null;
            if (decimal.TryParse(token, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out _)) return token;
            throw Error("metin, sayı veya NULL");
        }
    }
}
