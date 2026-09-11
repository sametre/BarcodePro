using BarcodePrinter;
using BarcodePrinter.Services.Network;
using Microsoft.Data.Sqlite;

internal static class SqliteInventoryChecks
{
    public static int Run(string directory)
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); count++; }
        void Reject(Action action, string name) { try { action(); } catch (InvalidOperationException) { Check(true, name); return; } throw new Exception(name); }
        var path = Path.Combine(directory, "concurrent.db");
        var first = new Inventory(path); var second = new Inventory(path);
        var p = new Product { Name="Concurrent", Barcode="00334455", Sku="CC-1", Stock=10, ImageData=[1,2,3], SourceImage="products/test.webp", SourceFields=new() { ["tax_rate"]="20",["deleted_at"]=null } };
        first.SaveProduct(p);
        // The second instance existed before the product was saved (same as service + admin UI).
        second.Move(p.Id, "Stok çıkışı", 2, "client B");
        first.Move(p.Id, "Stok çıkışı", 3, "client A");
        second.Refresh();
        Check(second.Data.Products.Single().Stock==5 && second.Data.Movements.Count==3, "Independent database instances read fresh stock and preserve every audit entry");
        Reject(()=>second.SaveProduct(new Product{Name="duplicate",Barcode=p.Barcode,Sku="different"}), "Cross-instance duplicate detection uses current database state");
        var reopened = new Inventory(path).Data.Products.Single();
        Check(reopened.ImageData!.SequenceEqual(new byte[]{1,2,3}) && reopened.SourceFields["tax_rate"]=="20" && reopened.SourceFields["deleted_at"]==null && reopened.SourceImage==p.SourceImage, "SQLite stores image bytes and source metadata losslessly");
        var parallel = Enumerable.Range(0,5).Select(i=>Task.Run(()=>new Inventory(path).Move(p.Id,"Stok çıkışı",1,"parallel "+i))).ToArray();
        Task.WaitAll(parallel); first.Refresh();
        Check(first.Data.Products.Single().Stock==0 && first.Data.Movements.Count==8, "Five concurrent stock deductions are serialized without lost updates");
        Reject(()=>second.Move(p.Id,"Stok çıkışı",1,"oversell"), "Stale client cannot oversell after other clients deplete stock");
        second.Move(p.Id,"Stok girişi",1,"restock");
        Reject(()=>first.Delete(p.Id), "Deletion checks latest stock inside its transaction");
        var prior = new Inventory(path).Data;
        using (var connection = new SqliteConnection("Data Source="+path))
        {
            connection.Open(); using var command=connection.CreateCommand();
            command.CommandText="CREATE TRIGGER test_audit_failure BEFORE INSERT ON Movements BEGIN SELECT RAISE(ABORT,'simulated audit write failure'); END;"; command.ExecuteNonQuery();
        }
        try { first.Move(p.Id,"Stok girişi",2,"rollback"); throw new Exception("Expected SQLite failure"); } catch(SqliteException) { }
        var after = new Inventory(path).Data;
        Check(after.Products.Single().Stock==prior.Products.Single().Stock && after.Movements.Count==prior.Movements.Count, "Database write failure rolls back both stock and audit atomically");
        using (var connection = new SqliteConnection("Data Source="+path)) { connection.Open(); using var command=connection.CreateCommand(); command.CommandText="DROP TRIGGER test_audit_failure";command.ExecuteNonQuery(); }
        var central = ServerDataMigration.Migrate(path, Path.Combine(directory,"service-data"));
        Check(new Inventory(central).Data.Movements.Count==prior.Movements.Count, "Service migration copies committed WAL data and audit history");
        first.Move(p.Id,"Stok girişi",1,"after migration");
        ServerDataMigration.Migrate(path,Path.GetDirectoryName(central)!);
        Check(new Inventory(central).Data.Products.Single().Stock==prior.Products.Single().Stock, "Service migration never overwrites an existing central database");
        var imageSource=new Product { Name="Reimport",Barcode=p.Barcode,Sku=p.Sku,Stock=0,Minimum=0,Maximum=100 };
        first.ImportProducts([imageSource],[nameof(Product.Name),nameof(Product.Barcode),nameof(Product.Sku),nameof(Product.ImageData),nameof(Product.SourceImage),nameof(Product.Stock)],false);
        var reimported=new Inventory(path).Data.Products.Single();
        Check(reimported.ImageData!.SequenceEqual(new byte[]{1,2,3}) && reimported.SourceImage==p.SourceImage && reimported.Stock==2,"Reimport without image files preserves existing embedded image and current stock");
        return count;
    }
}
