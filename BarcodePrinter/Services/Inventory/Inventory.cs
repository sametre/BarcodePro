using System.Text.RegularExpressions;
using BarcodePrinter.Services.Network;
namespace BarcodePrinter;
public sealed class Inventory
{
    private readonly SqliteInventoryStore? store;
    private readonly RemoteInventoryClient? remote;
    private readonly object gate = new();
    public InventoryData Data { get; private set; }
    public bool IsRemote => remote != null;
    public string Revision => store?.Revision() ?? Data.Revision;
    public string DatabasePath => store?.Path ?? "Barcode Pro Server / SQLite";
    public static readonly string[] MovementKinds = ["Stok girişi", "Stok çıkışı", "Stok düzeltme", "İade", "Fire"];
    public Inventory(string path)
    {
        string? legacy = Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) ? path : null;
        store = new SqliteInventoryStore(legacy == null ? path : Path.ChangeExtension(path, ".db"), legacy);
        Data = store.Load();
    }
    public Inventory(RemoteInventoryClient remote) { this.remote = remote; Data = remote.Snapshot(); }
    public void Refresh() { lock (gate) Data = remote?.SnapshotIfChanged(Data) ?? store!.Load(); }
    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); Refresh(); }, cancellationToken);
    public void BackupTo(string destination) { if (store == null) throw new InvalidOperationException("Yedek Server üzerinden alınmalıdır."); lock (gate) store.BackupTo(destination); }
    public static bool ValidBarcode(string value) => Regex.IsMatch(value, @"^[A-Za-z0-9\-\.\$/+% ]{4,64}$") && !string.IsNullOrWhiteSpace(value);
    private void Commit(Action<InventoryData> action) { lock (gate) Data = store!.Update(action); }
    public void SaveProduct(Product input)
    {
        if(remote!=null){lock(gate)Data=remote.Save(input.Copy());return;}
        var p = input.Copy();
        p.Name = p.Name.Trim(); p.Barcode = p.Barcode.Trim(); p.Sku = p.Sku.Trim();
        if (p.Name.Length == 0 || p.Sku.Length == 0) throw new InvalidOperationException("Ürün adı ve SKU zorunludur.");
        if (!ValidBarcode(p.Barcode)) throw new InvalidOperationException("Barkod 4–64 karakter olmalı; harf, rakam veya standart Code 39 sembolleri içermelidir.");
        if (p.Cost < 0 || p.Price < 0 || p.OldPrice < 0 || p.Stock < 0 || p.Minimum < 0 || p.Maximum < p.Minimum) throw new InvalidOperationException("Fiyat ve stok negatif olamaz. Maksimum stok minimumdan küçük olamaz.");
        Commit(d =>
        {
            if (d.Products.Any(x => x.Id != p.Id && x.Barcode.Equals(p.Barcode, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Bu barkod başka bir ürüne ait.");
            if (d.Products.Any(x => x.Id != p.Id && x.Sku.Equals(p.Sku, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Bu SKU başka bir ürüne ait.");
            var old = d.Products.FirstOrDefault(x => x.Id == p.Id);
            if (old != null) { p.Stock = old.Stock; p.CreatedAt = old.CreatedAt; d.Products.Remove(old); }
            p.UpdatedAt = DateTime.Now;
            d.Products.Add(p);
            if (old == null && p.Stock > 0) d.Movements.Add(new Movement { ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode, Kind = "Açılış stoğu", Delta = p.Stock, After = p.Stock });
        });
    }

    public void Move(Guid id, string kind, decimal quantity, string note)
    {
        if(remote!=null){lock(gate)Data=remote.Move(id,kind,quantity,note);return;}
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
        if(remote!=null){lock(gate)Data=remote.Delete(id);return;}
        Commit(d =>
        {
            var product = d.Products.Single(x => x.Id == id);
            if (product.Stock != 0) throw new InvalidOperationException("Silmek için önce stok sıfırlanmalıdır. Bunun yerine ürünü pasife alabilirsiniz.");
            d.Products.RemoveAll(x => x.Id == id);
        });
    }

    public (int Added,int Updated) ImportProducts(IEnumerable<Product> products,IReadOnlyCollection<string> mappedFields,bool updateStock)
    {
        if(remote!=null)throw new InvalidOperationException("MySQL aktarımı Server uygulamasından yapılmalıdır.");
        var rows=products.Select(p=>p.Copy()).ToList();
        var allowed=new MySqlSourceSettings().Columns.Keys.ToHashSet();
        allowed.UnionWith([nameof(Product.SourceFields),nameof(Product.ImageData),nameof(Product.SourceImage),nameof(Product.CreatedAt),nameof(Product.UpdatedAt)]);
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
                var old=byBarcode??bySku;var next=old?.Copy()??new Product{Id=row.Id};var before=next.Stock;
                if(old==null&&byId.ContainsKey(next.Id))throw new InvalidOperationException("Kaynak ürün kimliği farklı bir yerel ürüne ait.");
                if(!touched.Add(next.Id))throw new InvalidOperationException("Birden fazla kaynak satırı aynı yerel ürüne eşleşiyor.");
                foreach(var field in mappedFields)
                {
                    if(field==nameof(Product.Stock)&&old!=null&&!updateStock)continue;
                    if(field==nameof(Product.ImageData)&&old!=null&&row.ImageData is not {Length:>0})continue;
                    if(field==nameof(Product.SourceImage)&&old!=null&&string.IsNullOrWhiteSpace(row.SourceImage))continue;
                    var prop=typeof(Product).GetProperty(field)!;prop.SetValue(next,prop.GetValue(row));
                }
                if(string.IsNullOrWhiteSpace(next.Name)||string.IsNullOrWhiteSpace(next.Sku)||!ValidBarcode(next.Barcode)||next.Price<0||next.Cost<0||next.OldPrice<0||next.Stock<0||next.Minimum<0||next.Maximum<next.Minimum)
                    throw new InvalidOperationException("Aktarımda geçersiz ad, barkod, SKU, fiyat veya stok sınırı var. Hiçbir ürün kaydedilmedi.");
                if(!mappedFields.Contains(nameof(Product.UpdatedAt)))next.UpdatedAt=DateTime.Now;
                if(old!=null){barcodeIndex.Remove(old.Barcode);skuIndex.Remove(old.Sku);updated++;}else added++;
                byId[next.Id]=next;barcodeIndex[next.Barcode]=next;skuIndex[next.Sku]=next;
                if(next.Stock!=before)d.Movements.Add(new Movement{ProductId=next.Id,ProductName=next.Name,Barcode=next.Barcode,Kind=old==null?"Açılış stoğu":"Stok düzeltme",Before=before,After=next.Stock,Delta=next.Stock-before,Note="Ürün içe aktarımı"});
            }
            d.Products=byId.Values.ToList();
            if(d.Products.GroupBy(p=>p.Barcode,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1)||d.Products.GroupBy(p=>p.Sku,StringComparer.OrdinalIgnoreCase).Any(g=>g.Count()>1))throw new InvalidOperationException("Aktarım ürün kimliklerinde çakışma oluşturuyor.");
        });
        return(added,updated);
    }

    public void SeedDemo()
    {
        if(remote!=null){lock(gate)Data=remote.Seed();return;}
        Commit(d =>
        {
            if (d.Products.Count != 0 || d.Movements.Count != 0) throw new InvalidOperationException("Örnek veriler yalnızca boş envantere eklenebilir.");
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
