using BarcodePrinter;
using SkiaSharp;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length>=3 && args[0]=="--customer-seed") { CustomerSeedPreparation.Run(args[1],args[2],args.Length>3?args[3]:null,args.Length>4?args[4]:null); return; }
        if(args.Length>=3 && args[0]=="--customer-import") { CustomerSeedPreparation.ImportExisting(args[1],args[2],args.Length>3?args[3]:null,args.Length>4?args[4]:null); return; }
        if(args.Length>=3 && args[0]=="--export-json") { CustomerSeedPreparation.ExportJson(args[1],args[2]); return; }
        if(args.Length == 2 && args[0] == "--brand-assets") { Directory.CreateDirectory(args[1]); File.WriteAllBytes(Path.Combine(args[1], "BarcodePro.ico"), BarcodePrinter.Themes.BrandAssets.CreateIcon()); using var mark = BarcodePrinter.Themes.BrandAssets.CreateMark(512); mark.Save(Path.Combine(args[1], "BarcodePro.png"), System.Drawing.Imaging.ImageFormat.Png); return; }
        if(args.Length == 3 && args[0] == "--jupiter-icon") { CreateJupiterIcon(args[1], args[2]); return; }
        var directory = Path.Combine(Path.GetTempPath(), "BarcodePro-checks-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        if(args.Contains("--storage-checks")) { Console.WriteLine($"{SqlImportChecks.Run(directory) + SqliteInventoryChecks.Run(directory)} storage checks passed."); return; }
        if(args.Contains("--network-checks")) { Console.WriteLine($"{NetworkChecks.Run(directory)} network checks passed."); return; }
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
        count += SqlImportChecks.Run(directory);
        count += SqliteInventoryChecks.Run(directory);
        Console.WriteLine($"{count} checks passed.");
    }

    private static void CreateJupiterIcon(string input, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        using var source = SKBitmap.Decode(input) ?? throw new InvalidDataException("AVIF görseli okunamadı.");
        using var square = new SKBitmap(512,512,SKColorType.Rgba8888,SKAlphaType.Premul);
        using(var canvas=new SKCanvas(square))
        {
            canvas.Clear(new SKColor(54,60,67));
            var side=Math.Min(source.Width,source.Height)*.68f;var src=new SKRect((source.Width-side)/2f,(source.Height-side)/2f,(source.Width+side)/2f,(source.Height+side)/2f);
            using var clip=new SKPath();clip.AddOval(new SKRect(34,34,478,478));canvas.Save();canvas.ClipPath(clip);canvas.DrawBitmap(source,src,new SKRect(34,34,478,478));canvas.Restore();
            using var pen=new SKPaint{Style=SKPaintStyle.Stroke,Color=new SKColor(190,196,202),StrokeWidth=8,IsAntialias=true};canvas.DrawOval(new SKRect(34,34,478,478),pen);
        }
        using var png=square.Encode(SKEncodedImageFormat.Png,100);File.WriteAllBytes(Path.Combine(outputDirectory,"R3-M-Kobi-Jupiter.png"),png.ToArray());
        var sizes=new[]{16,24,32,48,64,128,256};var images=new List<byte[]>();foreach(var size in sizes){using var bmp=new SKBitmap(size,size);using(var c=new SKCanvas(bmp)){c.DrawBitmap(square,new SKRect(0,0,size,size));}using var encoded=bmp.Encode(SKEncodedImageFormat.Png,100);images.Add(encoded.ToArray());}
        using var result=new MemoryStream();using var writer=new BinaryWriter(result);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}foreach(var image in images)writer.Write(image);File.WriteAllBytes(Path.Combine(outputDirectory,"BarcodePro.ico"),result.ToArray());
    }
}






