using BarcodePrinter;
using SkiaSharp;

internal static class SqlImportChecks
{
    public static int Run(string directory)
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
        void Reject(Action action, string name) { try { action(); } catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException) { Check(true, name); return; } throw new Exception(name); }
        const string sql = "INSERT INTO `laravel`.`products` (`name`,code,barcode,category_id,brand_id,unit_id,purchase_price,sale_price,tax_rate,min_stock,current_stock,location,description,image,is_active,created_at,updated_at,deleted_at) VALUES ('Ürün O''Brien','SKU-1','00001234',2,4,1,12.50,23.90,20,2,9,'A1','quoted \\' text; DROP TABLE Products;','products/test.webp',1,'2026-07-01 19:40:55','2026-07-02 11:20:00',NULL), ('İkinci','SKU-2',NULL,3,2,1,1,2,10,0,3,NULL,NULL,NULL,1,'2026-07-01 10:00:00','2026-07-01 10:00:00',NULL);";
        var result = ProductSqlImport.Parse(sql, "test.sql");
        var first = result.Products[0];
        Check(result.Rows == 2 && result.ImagesReferenced == 1 && result.ImagesMissing == 1 && result.ImagesUnspecified == 1, "SQL preview reports every product and missing image accurately");
        Check(first.Name == "Ürün O'Brien" && first.Description == "quoted ' text; DROP TABLE Products;" && first.Barcode == "00001234", "SQL literal reader preserves Unicode, apostrophes, leading zeros and treats SQL inside strings as data");
        Check(first.Price == 23.90m && first.Stock == 9 && first.SourceFields["tax_rate"] == "20" && first.SourceFields["brand_id"] == "4" && first.SourceFields["location"] == "A1" && first.SourceFields["deleted_at"] == null, "SQL import preserves prices, stock and all source metadata");
        Check(first.CreatedAt == new DateTime(2026, 7, 1, 19, 40, 55) && first.UpdatedAt == new DateTime(2026, 7, 2, 11, 20, 0), "SQL import preserves source timestamps");
        var repeat = ProductSqlImport.Parse(sql, "renamed.sql");
        Check(result.Products[1].Barcode == repeat.Products[1].Barcode && Inventory.ValidBarcode(result.Products[1].Barcode) && first.Id == repeat.Products[0].Id, "Missing barcode fallback and product IDs are deterministic across imports");
        var invalidBarcode = ProductSqlImport.Parse("INSERT INTO products (name,code,barcode) VALUES ('Ürün','code','Türkçe ürün adı');");
        Check(Inventory.ValidBarcode(invalidBarcode.Products.Single().Barcode) && invalidBarcode.Products.Single().SourceFields["barcode"] == "Türkçe ürün adı", "Unsupported legacy barcode gets printable fallback while original is retained");
        Reject(() => ProductSqlImport.Parse(sql + "DROP TABLE Products;"), "Executable trailing SQL rejected without executing any statement");
        Reject(() => ProductSqlImport.Parse("INSERT INTO users (name) VALUES ('admin');"), "Non-product tables rejected");
        Reject(() => ProductSqlImport.Parse("INSERT INTO products (name,barcode) VALUES ('test',CONCAT('1','2'));"), "SQL expressions rejected; only literal values accepted");
        Reject(() => ProductSqlImport.Parse("INSERT INTO products (name,barcode) VALUES ('first','1234'),('second','1234');"), "Duplicate source barcodes reject import without silently merging products");
        var duplicateSku = ProductSqlImport.Parse("INSERT INTO products (name,code,barcode) VALUES ('first','same','1234'),('second','same','5678');");
        Check(duplicateSku.Rows == 2 && duplicateSku.Products[0].Sku != duplicateSku.Products[1].Sku && duplicateSku.Products[1].SourceFields["code"] == "same", "Duplicate source SKU receives stable suffix while preserving original code and both products");
        var deleted = ProductSqlImport.Parse("INSERT INTO products (name,code,barcode,is_active,deleted_at) VALUES ('old','old','9999',1,'2026-08-01 00:00:00');");
        Check(!deleted.Products.Single().Active && !deleted.Products.Single().OnMenu, "Soft-deleted source products are retained as inactive");
        string imageDirectory = Path.Combine(directory, "sql-images"); Directory.CreateDirectory(Path.Combine(imageDirectory, "products"));
        using (var bitmap = new SKBitmap(24, 24))
        {
            using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.SlateBlue);
            using var image = SKImage.FromBitmap(bitmap); using var bytes = image.Encode(SKEncodedImageFormat.Webp, 90);
            File.WriteAllBytes(Path.Combine(imageDirectory, "products", "test.webp"), bytes.ToArray());
        }
        ProductSqlImport.LoadImagesAsync(result, imageDirectory).GetAwaiter().GetResult();
        Check(result.ImagesLoaded == 1 && result.ImagesMissing == 0 && first.ImageData is { Length: > 0 }, "WebP product image normalized and embedded without absolute client paths");
        using (var imageStream = new MemoryStream(first.ImageData!)) using (var image = System.Drawing.Image.FromStream(imageStream)) Check(image.Width == 24 && image.Height == 24, "Embedded image can be rendered by WinForms on every client");
        Reject(() => ProductSqlImport.ResolveImageFile(imageDirectory, "../outside.jpg"), "Image import rejects path traversal outside selected folder");
        var copy = first.Copy(); copy.SourceFields["name"] = "changed"; copy.ImageData![0] = 0;
        Check(first.SourceFields["name"] == "Ürün O'Brien" && first.ImageData![0] != 0, "Product copies isolate source metadata and embedded image bytes");
        return count;
    }
}
