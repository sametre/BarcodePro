using BarcodePrinter;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text.Json;

internal static class CustomerSeedPreparation
{
    public static void ExportJson(string databasePath, string jsonPath)
    {
        var inventory = new Inventory(databasePath);
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        File.WriteAllText(Path.GetFullPath(jsonPath), JsonSerializer.Serialize(inventory.Data, options));
        Console.WriteLine($"JSON exported: {inventory.Data.Products.Count} products, {inventory.Data.Movements.Count} movements -> {Path.GetFullPath(jsonPath)}");
    }
    public static void ImportExisting(string sqlPath, string targetDatabase, string? imageDirectory = null, string? imageBaseUrl = null)
    {
        var parsed = ProductSqlImport.ReadFile(sqlPath);
        ProductSqlImport.LoadImagesAsync(parsed, imageDirectory, string.IsNullOrWhiteSpace(imageBaseUrl) ? null : new Uri(imageBaseUrl)).GetAwaiter().GetResult();
        string database = Path.GetFullPath(targetDatabase);
        var inventory = new Inventory(database);
        string? backup = null;
        if (inventory.Data.Products.Count > 0 || inventory.Data.Movements.Count > 0)
        {
            backup = database + ".before-import-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".db";
            inventory.BackupTo(backup);
        }
        var imported = inventory.ImportProducts(parsed.Products, parsed.MappedFields, false);
        var stored = new Inventory(database);
        foreach (var product in parsed.Products)
        {
            var match = stored.Data.Products.SingleOrDefault(p => p.Barcode.Equals(product.Barcode, StringComparison.OrdinalIgnoreCase));
            if (match == null || !match.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase) || product.SourceFields.Any(field => !match.SourceFields.TryGetValue(field.Key, out var value) || value != field.Value))
                throw new InvalidOperationException("İçe aktarılan ürünlerden biri yeniden okunduğunda doğrulanamadı.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Database = database, Backup = backup, parsed.Rows, imported.Added, imported.Updated,
            TotalProducts = stored.Data.Products.Count, parsed.ImagesLoaded, parsed.ImagesMissing, parsed.ImagesUnspecified,
            Verified = "All source products and metadata verified; existing local stock retained", parsed.Warnings
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Run(string sqlPath, string outputDirectory, string? imageDirectory = null, string? imageBaseUrl = null)
    {
        string output = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(output);
        var parsed = ProductSqlImport.ReadFile(sqlPath);
        ProductSqlImport.LoadImagesAsync(parsed, imageDirectory, string.IsNullOrWhiteSpace(imageBaseUrl) ? null : new Uri(imageBaseUrl)).GetAwaiter().GetResult();
        string databasePath = Path.Combine(output, "inventory.db");
        if (File.Exists(databasePath)) throw new InvalidOperationException("Hedef seed veritabanı zaten var; önce yeni boş çıktı klasörü seçin.");
        var inventory = new Inventory(databasePath);
        var first = inventory.ImportProducts(parsed.Products, parsed.MappedFields, true);
        int movements = inventory.Data.Movements.Count;
        var second = inventory.ImportProducts(parsed.Products, parsed.MappedFields, true);
        var reopened = new Inventory(databasePath);
        if (first.Added != parsed.Rows || second.Added != 0 || second.Updated != parsed.Rows || reopened.Data.Products.Count != parsed.Rows || reopened.Data.Movements.Count != movements)
            throw new InvalidOperationException("Seed doğrulaması başarısız: ürün sayısı veya tekrar aktarım tutarlılığı eşleşmiyor.");
        foreach (var source in parsed.Products)
        {
            var stored = reopened.Data.Products.Single(p => p.Barcode == source.Barcode);
            if (stored.SourceFields.Count != source.SourceFields.Count || source.SourceFields.Any(field => !stored.SourceFields.TryGetValue(field.Key, out var value) || value != field.Value) || stored.CreatedAt != source.CreatedAt || stored.UpdatedAt != source.UpdatedAt || stored.Stock != source.Stock)
                throw new InvalidOperationException("Seed doğrulaması başarısız: kaynak alanı veya tarih kaybı var.");
        }
        using (var connection = new SqliteConnection("Data Source=" + databasePath))
        {
            connection.Open();
            using var checkpoint = connection.CreateCommand(); checkpoint.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);"; checkpoint.ExecuteNonQuery();
            using var integrity = connection.CreateCommand(); integrity.CommandText = "PRAGMA integrity_check;";
            if (Convert.ToString(integrity.ExecuteScalar()) != "ok") throw new InvalidOperationException("Seed SQLite bütünlük kontrolü başarısız.");
        }
        SqliteConnection.ClearAllPools();
        var report = new
        {
            SourceFile = Path.GetFileName(sqlPath), SourceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sqlPath))),
            parsed.Rows, parsed.ImagesReferenced, parsed.ImagesLoaded, parsed.ImagesMissing, parsed.ImagesUnspecified,
            TotalStock = parsed.Products.Sum(p => p.Stock), OpeningMovements = movements,
            MissingBarcodes = parsed.Products.Count(p => string.IsNullOrWhiteSpace(p.SourceFields.GetValueOrDefault("barcode"))),
            UnsupportedBarcodes = parsed.Products.Count(p => !string.IsNullOrWhiteSpace(p.SourceFields.GetValueOrDefault("barcode")) && !Inventory.ValidBarcode(p.SourceFields["barcode"]!)),
            UniqueSkus = parsed.Products.Select(p => p.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            UniqueBarcodes = parsed.Products.Select(p => p.Barcode).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            SourceCategories = parsed.Products.Select(p => p.SourceFields.GetValueOrDefault("category_id")).Distinct().Order().ToArray(),
            SourceUnits = parsed.Products.Select(p => p.SourceFields.GetValueOrDefault("unit_id")).Distinct().Order().ToArray(),
            SourceImageExtensions = parsed.Products.Where(p => p.SourceImage.Length > 0).GroupBy(p => Path.GetExtension(p.SourceImage)).ToDictionary(g => g.Key, g => g.Count()),
            parsed.Warnings,
            Verified = "SQLite integrity_check=ok; all rows and source fields preserved; repeated import does not duplicate products or movements"
        };
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "import-report.json"), json);
        Console.WriteLine(json);
        Console.WriteLine("Seed database: " + databasePath);
    }
}
