using BarcodePrinter;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length == 2 && args[0] == "--brand-assets") { Directory.CreateDirectory(args[1]); File.WriteAllBytes(Path.Combine(args[1], "BarcodePro.ico"), BarcodePrinter.Themes.BrandAssets.CreateIcon()); using var mark = BarcodePrinter.Themes.BrandAssets.CreateMark(512); mark.Save(Path.Combine(args[1], "BarcodePro.png"), System.Drawing.Imaging.ImageFormat.Png); return; }
        var directory = Path.Combine(Path.GetTempPath(), "BarcodePro-checks-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "inventory.json");
        var store = new Inventory(path);
        var p = new Product { Name = "Test", Barcode = "8691234567890", Sku = "TEST-1", Stock = 10 };
        int count = 0;
        void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
        void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true, name); return; } throw new Exception("Expected rejection: " + name); }
        store.SaveProduct(p);
        Check(store.Data.Movements.Count == 1, "Opening balance is audited");
        Reject(() => store.SaveProduct(new Product { Name = "Duplicate", Barcode = p.Barcode, Sku = "TEST-2" }), "Duplicate barcode");
        Reject(() => store.SaveProduct(new Product { Name = "Duplicate", Barcode = "123456", Sku = p.Sku }), "Duplicate SKU");
        store.Move(p.Id, "Stok çıkışı", 3, "sale");
        Check(store.Data.Products.Single().Stock == 7, "Stock out");
        Reject(() => store.Move(p.Id, "Stok çıkışı", 8, ""), "Overselling prevented");
        Check(new Inventory(path).Data.Products.Single().Stock == 7, "Rejected operation leaves disk unchanged");
        store.Move(p.Id, "İade", 2, "return"); store.Move(p.Id, "Fire", 1, "broken");
        Check(store.Data.Products.Single().Stock == 8, "Return and waste");
        Reject(() => store.Move(p.Id, "Stok düzeltme", 5, ""), "Correction requires reason");
        store.Move(p.Id, "Stok düzeltme", 5, "count");
        Check(store.Data.Products.Single().Stock == 5 && store.Data.Movements.Last().Delta == -3, "Correction sets absolute stock");
        var edited = store.Data.Products.Single().Copy(); edited.Stock = 900; store.SaveProduct(edited);
        Check(store.Data.Products.Single().Stock == 5, "Product edit cannot bypass stock ledger");
        edited.Active = false; store.SaveProduct(edited);
        Reject(() => store.Move(p.Id, "Stok girişi", 1, ""), "Inactive stock movement");
        Reject(() => store.Delete(p.Id), "Nonzero stock deletion");
        edited.Active = true; store.SaveProduct(edited); store.Move(p.Id, "Stok düzeltme", 0, "close"); store.Delete(p.Id);
        Check(store.Data.Products.Count == 0 && store.Data.Movements.Count == 6, "Deletion retains movement history");
        var sqlitePath=Path.ChangeExtension(path,".db");Check(File.Exists(sqlitePath), "SQLite database created");
        using(var sqlite=new Microsoft.Data.Sqlite.SqliteConnection("Data Source="+sqlitePath)){sqlite.Open();using var command=sqlite.CreateCommand();command.CommandText="SELECT COUNT(*) FROM Movements";Check(Convert.ToInt32(command.ExecuteScalar())==6,"SQLite CRUD movement audit persisted");}
        Check(!Inventory.ValidBarcode("<script>") && Inventory.ValidBarcode("00001234"), "Barcode validation and leading zeros");
        var failurePath = Path.Combine(directory, "blocked.json");Directory.CreateDirectory(Path.ChangeExtension(failurePath,".db"));
        try { _=new Inventory(failurePath); throw new Exception("Expected database open failure"); } catch (Microsoft.Data.Sqlite.SqliteException) { Check(true,"SQLite open failure reported without data loss"); }
        ApplicationConfiguration.Initialize();
        using var form = new Form1(Path.Combine(directory, "ui.json"));
        form.Show(); Application.DoEvents();
        foreach (var page in new[] { "Genel bakış", "Ürünler", "Barkod işlemleri", "Stok hareketleri", "Menü ve durum", "Ayarlar" })
        {
            typeof(Form1).GetMethod("ShowPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(form, [page]);
            Application.DoEvents(); Check(form.Controls.Count > 0, "UI page " + page);
        }
        form.Close();
        count += PrintingChecks.Run(directory);
        count += ConnectionChecks.Run(directory);
        count += BetaChecks.Run(directory);
        count += PrinterRoutingChecks.Run();
        count += NetworkChecks.Run(directory);
        Console.WriteLine($"{count} checks passed.");
    }
}






