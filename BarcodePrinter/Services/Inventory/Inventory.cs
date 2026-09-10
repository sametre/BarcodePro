using System.Text.Json;
using System.Text.RegularExpressions;
using BarcodePrinter.Services.Network;
using Microsoft.Data.Sqlite;
namespace BarcodePrinter;
public sealed class Inventory
{
    private readonly string path;
    private readonly string? legacyJsonPath;
    private readonly RemoteInventoryClient? remote;
    public InventoryData Data { get; private set; }
    public string DatabasePath => remote==null?path:"Barcode Pro Server / SQLite";
    public static readonly string[] MovementKinds = ["Stok girişi", "Stok çıkışı", "Stok düzeltme", "İade", "Fire"];
    public Inventory(string path)
    {
        legacyJsonPath=Path.GetExtension(path).Equals(".json",StringComparison.OrdinalIgnoreCase)?path:null;
        this.path = legacyJsonPath==null?path:Path.ChangeExtension(path,".db");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(this.path))!);
        bool migrate=!File.Exists(this.path)&&legacyJsonPath!=null&&File.Exists(legacyJsonPath);
        InitializeDatabase();Data=migrate?LoadLegacy(legacyJsonPath!):LoadDatabase();
        if(migrate)
        {
            try{Persist(Data);File.Copy(legacyJsonPath!,legacyJsonPath!+".migrated.bak",true);}
            catch{SqliteConnection.ClearAllPools();foreach(var suffix in new[]{"","-wal","-shm"})try{File.Delete(this.path+suffix);}catch{}throw;}
        }
    }
    public Inventory(RemoteInventoryClient remote)
    {
        this.remote=remote;path="";Data=remote.Snapshot();
    }

    private void RefreshRemote(){if(remote!=null)Data=remote.Snapshot();}

    public static bool ValidBarcode(string value) => Regex.IsMatch(value, @"^[A-Za-z0-9\-\.\$/+% ]{4,64}$") && !string.IsNullOrWhiteSpace(value);

    private void Commit(Action<InventoryData> action)
    {
        var next = JsonSerializer.Deserialize<InventoryData>(JsonSerializer.Serialize(Data))!;
        action(next);
        Persist(next);
        Data = next;
    }

    private SqliteConnection Open()
    {
        var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadWriteCreate,Cache=SqliteCacheMode.Shared,Pooling=true}.ConnectionString);
        connection.Open();using var pragma=connection.CreateCommand();pragma.CommandText="PRAGMA foreign_keys=ON; PRAGMA busy_timeout=10000;";pragma.ExecuteNonQuery();return connection;
    }
    private void InitializeDatabase()
    {
        using var connection=Open();using var command=connection.CreateCommand();command.CommandText="""
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Products(Id TEXT PRIMARY KEY,Name TEXT NOT NULL,Barcode TEXT NOT NULL COLLATE NOCASE,Sku TEXT NOT NULL COLLATE NOCASE,Category TEXT NOT NULL,Cost TEXT NOT NULL,Price TEXT NOT NULL,OldPrice TEXT NULL,Stock TEXT NOT NULL,Minimum TEXT NOT NULL,Maximum TEXT NOT NULL,Unit TEXT NOT NULL,Description TEXT NOT NULL,ImagePath TEXT NOT NULL,OnMenu INTEGER NOT NULL,Active INTEGER NOT NULL,CreatedAt TEXT NOT NULL,UpdatedAt TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Barcode ON Products(Barcode COLLATE NOCASE);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Sku ON Products(Sku COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS Movements(Id TEXT PRIMARY KEY,ProductId TEXT NOT NULL,ProductName TEXT NOT NULL,Barcode TEXT NOT NULL,Kind TEXT NOT NULL,BeforeAmount TEXT NOT NULL,Delta TEXT NOT NULL,AfterAmount TEXT NOT NULL,Note TEXT NOT NULL,At TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_Movements_ProductId ON Movements(ProductId);
            CREATE INDEX IF NOT EXISTS IX_Movements_At ON Movements(At DESC);
            """;command.ExecuteNonQuery();
    }
    private static InventoryData LoadLegacy(string jsonPath)=>JsonSerializer.Deserialize<InventoryData>(File.ReadAllText(jsonPath))??throw new InvalidDataException("Eski envanter dosyası boş.");
    private InventoryData LoadDatabase()
    {
        var data=new InventoryData();using var connection=Open();using(var command=connection.CreateCommand())
        {
            command.CommandText="SELECT Id,Name,Barcode,Sku,Category,Cost,Price,OldPrice,Stock,Minimum,Maximum,Unit,Description,ImagePath,OnMenu,Active,CreatedAt,UpdatedAt FROM Products ORDER BY Name";
            using var r=command.ExecuteReader();while(r.Read())data.Products.Add(new Product{Id=Guid.Parse(r.GetString(0)),Name=r.GetString(1),Barcode=r.GetString(2),Sku=r.GetString(3),Category=r.GetString(4),Cost=Decimal(r,5),Price=Decimal(r,6),OldPrice=r.IsDBNull(7)?null:Decimal(r,7),Stock=Decimal(r,8),Minimum=Decimal(r,9),Maximum=Decimal(r,10),Unit=r.GetString(11),Description=r.GetString(12),ImagePath=r.GetString(13),OnMenu=r.GetInt64(14)!=0,Active=r.GetInt64(15)!=0,CreatedAt=DateTime.Parse(r.GetString(16),null,System.Globalization.DateTimeStyles.RoundtripKind),UpdatedAt=DateTime.Parse(r.GetString(17),null,System.Globalization.DateTimeStyles.RoundtripKind)});
        }
        using(var command=connection.CreateCommand())
        {
            command.CommandText="SELECT Id,ProductId,ProductName,Barcode,Kind,BeforeAmount,Delta,AfterAmount,Note,At FROM Movements ORDER BY At";
            using var r=command.ExecuteReader();while(r.Read())data.Movements.Add(new Movement{Id=Guid.Parse(r.GetString(0)),ProductId=Guid.Parse(r.GetString(1)),ProductName=r.GetString(2),Barcode=r.GetString(3),Kind=r.GetString(4),Before=Decimal(r,5),Delta=Decimal(r,6),After=Decimal(r,7),Note=r.GetString(8),At=DateTime.Parse(r.GetString(9),null,System.Globalization.DateTimeStyles.RoundtripKind)});
        }
        return data;
    }
    private static decimal Decimal(SqliteDataReader reader,int index)=>decimal.Parse(reader.GetString(index),System.Globalization.CultureInfo.InvariantCulture);
    private void Persist(InventoryData data)
    {
        using var connection=Open();using var transaction=connection.BeginTransaction();
        using(var clear=connection.CreateCommand()){clear.Transaction=transaction;clear.CommandText="DELETE FROM Products; DELETE FROM Movements;";clear.ExecuteNonQuery();}
        foreach(var p in data.Products)
        {
            using var c=connection.CreateCommand();c.Transaction=transaction;c.CommandText="INSERT INTO Products VALUES($Id,$Name,$Barcode,$Sku,$Category,$Cost,$Price,$OldPrice,$Stock,$Minimum,$Maximum,$Unit,$Description,$ImagePath,$OnMenu,$Active,$CreatedAt,$UpdatedAt)";
            Add(c,"$Id",p.Id.ToString());Add(c,"$Name",p.Name);Add(c,"$Barcode",p.Barcode);Add(c,"$Sku",p.Sku);Add(c,"$Category",p.Category);Add(c,"$Cost",Number(p.Cost));Add(c,"$Price",Number(p.Price));Add(c,"$OldPrice",p.OldPrice.HasValue?Number(p.OldPrice.Value):DBNull.Value);Add(c,"$Stock",Number(p.Stock));Add(c,"$Minimum",Number(p.Minimum));Add(c,"$Maximum",Number(p.Maximum));Add(c,"$Unit",p.Unit);Add(c,"$Description",p.Description);Add(c,"$ImagePath",p.ImagePath);Add(c,"$OnMenu",p.OnMenu?1:0);Add(c,"$Active",p.Active?1:0);Add(c,"$CreatedAt",p.CreatedAt.ToString("O"));Add(c,"$UpdatedAt",p.UpdatedAt.ToString("O"));c.ExecuteNonQuery();
        }
        foreach(var m in data.Movements)
        {
            using var c=connection.CreateCommand();c.Transaction=transaction;c.CommandText="INSERT INTO Movements VALUES($Id,$ProductId,$ProductName,$Barcode,$Kind,$Before,$Delta,$After,$Note,$At)";
            Add(c,"$Id",m.Id.ToString());Add(c,"$ProductId",m.ProductId.ToString());Add(c,"$ProductName",m.ProductName);Add(c,"$Barcode",m.Barcode);Add(c,"$Kind",m.Kind);Add(c,"$Before",Number(m.Before));Add(c,"$Delta",Number(m.Delta));Add(c,"$After",Number(m.After));Add(c,"$Note",m.Note);Add(c,"$At",m.At.ToString("O"));c.ExecuteNonQuery();
        }
        transaction.Commit();
    }
    private static string Number(decimal value)=>value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    private static void Add(SqliteCommand command,string name,object value)=>command.Parameters.AddWithValue(name,value);

    public void SaveProduct(Product input)
    {
        if(remote!=null){remote.Save(input.Copy());RefreshRemote();return;}
        var p = input.Copy();
        p.Name = p.Name.Trim(); p.Barcode = p.Barcode.Trim(); p.Sku = p.Sku.Trim();
        if (p.Name.Length == 0 || p.Sku.Length == 0) throw new InvalidOperationException("Ürün adı ve SKU zorunludur.");
        if (!ValidBarcode(p.Barcode)) throw new InvalidOperationException("Barkod 4–64 karakter olmalı; harf, rakam veya standart Code 39 sembolleri içermelidir.");
        if (Data.Products.Any(x => x.Id != p.Id && x.Barcode.Equals(p.Barcode, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Bu barkod başka bir ürüne ait.");
        if (Data.Products.Any(x => x.Id != p.Id && x.Sku.Equals(p.Sku, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Bu SKU başka bir ürüne ait.");
        if (p.Cost < 0 || p.Price < 0 || p.OldPrice < 0 || p.Stock < 0 || p.Minimum < 0 || p.Maximum < p.Minimum) throw new InvalidOperationException("Fiyat ve stok negatif olamaz. Maksimum stok minimumdan küçük olamaz.");
        Commit(d =>
        {
            var old = d.Products.FirstOrDefault(x => x.Id == p.Id);
            if (old != null) { p.Stock = old.Stock; p.CreatedAt = old.CreatedAt; d.Products.Remove(old); }
            p.UpdatedAt = DateTime.Now;
            d.Products.Add(p);
            if (old == null && p.Stock > 0) d.Movements.Add(new Movement { ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode, Kind = "Açılış stoğu", Delta = p.Stock, After = p.Stock });
        });
    }

    public void Move(Guid id, string kind, decimal quantity, string note)
    {
        if(remote!=null){remote.Move(id,kind,quantity,note);RefreshRemote();return;}
        if (!MovementKinds.Contains(kind)) throw new InvalidOperationException("Geçersiz işlem türü.");
        if (quantity < 0 || (quantity == 0 && kind != "Stok düzeltme")) throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");
        if ((kind == "Stok düzeltme" || kind == "Fire") && string.IsNullOrWhiteSpace(note)) throw new InvalidOperationException("Düzeltme ve fire için açıklama zorunludur.");
        Commit(d =>
        {
            var p = d.Products.Single(x => x.Id == id);
            if (!p.Active) throw new InvalidOperationException("Pasif ürün için stok hareketi yapılamaz.");
            var delta = kind switch { "Stok çıkışı" or "Fire" => -quantity, "Stok düzeltme" => quantity - p.Stock, _ => quantity };
            var after = p.Stock + delta;
            if (after < 0) throw new InvalidOperationException("Yetersiz stok. İşlem mevcut stoğu aşamaz.");
            if (delta == 0) throw new InvalidOperationException("Yeni stok mevcut stokla aynı.");
            d.Movements.Add(new Movement { ProductId = id, ProductName = p.Name, Barcode = p.Barcode, Kind = kind, Before = p.Stock, Delta = delta, After = after, Note = note.Trim() });
            p.Stock = after; p.UpdatedAt = DateTime.Now;
        });
    }

    public void Delete(Guid id)
    {
        if(remote!=null){remote.Delete(id);RefreshRemote();return;}
        var product = Data.Products.Single(x => x.Id == id);
        if (product.Stock != 0) throw new InvalidOperationException("Silmek için önce stok sıfırlanmalıdır. Bunun yerine ürünü pasife alabilirsiniz.");
        Commit(d => d.Products.RemoveAll(x => x.Id == id));
    }

    public (int Added,int Updated) ImportProducts(IEnumerable<Product> products,IReadOnlyCollection<string> mappedFields,bool updateStock)
    {
        if(remote!=null)throw new InvalidOperationException("MySQL aktarımı Server uygulamasından yapılmalıdır.");
        var rows=products.Select(p=>p.Copy()).ToList();
        var allowed=new MySqlSourceSettings().Columns.Keys.ToHashSet();
        if(mappedFields.Any(f=>!allowed.Contains(f)))throw new InvalidOperationException("Geçersiz aktarım alanı.");
        if(new[]{nameof(Product.Name),nameof(Product.Barcode),nameof(Product.Sku)}.Any(f=>!mappedFields.Contains(f)))throw new InvalidOperationException("Ad, barkod ve SKU eşleştirmesi zorunlu.");
        if(rows.Count==0)return(0,0);
        int added=0,updated=0;
        Commit(d=>
        {
            var barcodes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var skus=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var byId=d.Products.ToDictionary(p=>p.Id);var barcodeIndex=d.Products.ToDictionary(p=>p.Barcode,StringComparer.OrdinalIgnoreCase);var skuIndex=d.Products.ToDictionary(p=>p.Sku,StringComparer.OrdinalIgnoreCase);var touched=new HashSet<Guid>();
            foreach(var row in rows)
            {
                row.Barcode=row.Barcode.Trim();row.Sku=row.Sku.Trim();
                if(!barcodes.Add(row.Barcode)||!skus.Add(row.Sku))throw new InvalidOperationException("Kaynakta tekrar eden barkod veya SKU var. Aktarım iptal edildi.");
                var byBarcode=barcodeIndex.GetValueOrDefault(row.Barcode);
                var bySku=skuIndex.GetValueOrDefault(row.Sku);
                if(byBarcode!=null&&bySku!=null&&byBarcode.Id!=bySku.Id)throw new InvalidOperationException("Barkod ve SKU farklı yerel ürünlerle eşleşiyor. Aktarım iptal edildi.");
                var old=byBarcode??bySku;var next=old?.Copy()??new Product();var before=next.Stock;
                if(!touched.Add(next.Id))throw new InvalidOperationException("Birden fazla kaynak satırı aynı yerel ürüne eşleşiyor.");
                foreach(var field in mappedFields)
                {
                    if(field==nameof(Product.Stock)&&old!=null&&!updateStock)continue;
                    var prop=typeof(Product).GetProperty(field)!;prop.SetValue(next,prop.GetValue(row));
                }
                if(string.IsNullOrWhiteSpace(next.Name)||string.IsNullOrWhiteSpace(next.Sku)||!ValidBarcode(next.Barcode)||next.Price<0||next.Cost<0||next.OldPrice<0||next.Stock<0||next.Minimum<0||next.Maximum<next.Minimum)
                    throw new InvalidOperationException("Aktarımda geçersiz ad, barkod, SKU, fiyat veya stok sınırı var. Hiçbir ürün kaydedilmedi.");
                next.UpdatedAt=DateTime.Now;
                if(old!=null){barcodeIndex.Remove(old.Barcode);skuIndex.Remove(old.Sku);updated++;}else added++;
                byId[next.Id]=next;barcodeIndex[next.Barcode]=next;skuIndex[next.Sku]=next;
                if(next.Stock!=before)d.Movements.Add(new Movement{ProductId=next.Id,ProductName=next.Name,Barcode=next.Barcode,Kind=old==null?"Açılış stoğu":"Stok düzeltme",Before=before,After=next.Stock,Delta=next.Stock-before,Note="MySQL ürün aktarımı"});
            }
            d.Products=byId.Values.ToList();
            if(d.Products.GroupBy(p=>p.Barcode,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1)||d.Products.GroupBy(p=>p.Sku,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))throw new InvalidOperationException("Aktarım ürün kimliklerinde çakışma oluşturuyor.");
        });
        return(added,updated);
    }

    public void SeedDemo()
    {
        if(remote!=null){remote.Seed();RefreshRemote();return;}
        if (Data.Products.Count != 0 || Data.Movements.Count != 0) throw new InvalidOperationException("Örnek veriler yalnızca boş envantere eklenebilir.");
        Commit(d =>
        {
            string[] names = ["Espresso çekirdeği", "Tam yağlı süt", "Vanilya şurubu", "Karton bardak 8 oz", "Çikolatalı kurabiye", "Yeşil çay", "Yulaf sütü", "Karamel sos"];
            string[] cats = ["Kahve", "Süt ürünleri", "Şuruplar", "Ambalaj", "Atıştırmalık", "Çay", "Süt ürünleri", "Şuruplar"];
            decimal[] stocks = [48, 8, 0, 240, 16, 32, 4, 12];
            for (int i = 0; i < names.Length; i++)
            {
                var p = new Product { Name = names[i], Barcode = "869000000000" + i, Sku = $"BP-{1001 + i}", Category = cats[i], Stock = stocks[i], Minimum = 10, Maximum = 300, Cost = 25 + i * 5, Price = 50 + i * 10, OnMenu = i != 3, Active = i != 7 };
                d.Products.Add(p);
                d.Movements.Add(new Movement { ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode, Kind = "Açılış stoğu", Delta = p.Stock, After = p.Stock, Note = "Örnek veri", At = DateTime.Now.AddDays(-6) });
            }
        });
    }
}


