using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace BarcodePrinter;

/// <summary>Each write reads the current state while holding SQLite's writer lock.</summary>
internal sealed class SqliteInventoryStore
{
    public string Path { get; }
    public SqliteInventoryStore(string path, string? legacyPath)
    {
        Path = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        using var connection = Open();
        using (var wal = connection.CreateCommand()) { wal.CommandText = "PRAGMA journal_mode=WAL;"; wal.ExecuteNonQuery(); }
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var schema = connection.CreateCommand())
        {
            schema.Transaction = transaction;
            schema.CommandText = """
                CREATE TABLE IF NOT EXISTS Products(Id TEXT PRIMARY KEY,Name TEXT NOT NULL,Barcode TEXT NOT NULL COLLATE NOCASE,Sku TEXT NOT NULL COLLATE NOCASE,Category TEXT NOT NULL,Cost TEXT NOT NULL,Price TEXT NOT NULL,OldPrice TEXT NULL,Stock TEXT NOT NULL,Minimum TEXT NOT NULL,Maximum TEXT NOT NULL,Unit TEXT NOT NULL,Description TEXT NOT NULL,ImagePath TEXT NOT NULL,OnMenu INTEGER NOT NULL,Active INTEGER NOT NULL,CreatedAt TEXT NOT NULL,UpdatedAt TEXT NOT NULL);
                CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Barcode ON Products(Barcode COLLATE NOCASE);
                CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Sku ON Products(Sku COLLATE NOCASE);
                CREATE TABLE IF NOT EXISTS Movements(Id TEXT PRIMARY KEY,ProductId TEXT NOT NULL,ProductName TEXT NOT NULL,Barcode TEXT NOT NULL,Kind TEXT NOT NULL,BeforeAmount TEXT NOT NULL,Delta TEXT NOT NULL,AfterAmount TEXT NOT NULL,Note TEXT NOT NULL,At TEXT NOT NULL);
                CREATE INDEX IF NOT EXISTS IX_Movements_ProductId ON Movements(ProductId);
                CREATE INDEX IF NOT EXISTS IX_Movements_At ON Movements(At DESC);
                CREATE TABLE IF NOT EXISTS InventoryMetadata(Key TEXT PRIMARY KEY,Value TEXT NOT NULL);
                """;
            schema.ExecuteNonQuery();
        }
        var columns = new HashSet<string>();
        using (var check = connection.CreateCommand())
        {
            check.Transaction = transaction; check.CommandText = "PRAGMA table_info(Products)";
            using var reader = check.ExecuteReader(); while (reader.Read()) columns.Add(reader.GetString(1));
        }
        foreach (var (name, type) in new[] { ("SourceFields", "TEXT NOT NULL DEFAULT '{}'"), ("ImageData", "BLOB NULL"), ("SourceImage", "TEXT NOT NULL DEFAULT ''") })
        {
            if (columns.Contains(name)) continue;
            using var alter = connection.CreateCommand(); alter.Transaction = transaction;
            alter.CommandText = $"ALTER TABLE Products ADD COLUMN {name} {type}"; alter.ExecuteNonQuery();
        }
        using var initialized = connection.CreateCommand(); initialized.Transaction = transaction;
        initialized.CommandText = "SELECT Value FROM InventoryMetadata WHERE Key='initialized'";
        if (initialized.ExecuteScalar() == null)
        {
            var current = Read(connection, transaction);
            if (current.Products.Count == 0 && current.Movements.Count == 0 && legacyPath != null && File.Exists(legacyPath))
            {
                var legacy = JsonSerializer.Deserialize<InventoryData>(File.ReadAllText(legacyPath)) ?? throw new InvalidDataException("Eski envanter dosyası boş.");
                if (!File.Exists(legacyPath + ".migrated.bak")) File.Copy(legacyPath, legacyPath + ".migrated.bak");
                WriteChanges(connection, transaction, current, legacy);
            }
            initialized.CommandText = "INSERT INTO InventoryMetadata(Key,Value) VALUES('initialized','1')"; initialized.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public string Revision()
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM InventoryMetadata WHERE Key='revision'";
        return command.ExecuteScalar() as string ?? "initial";
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Mode = SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 30, Pooling = true }.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand(); command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=30000;"; command.ExecuteNonQuery();
        return connection;
    }

    public InventoryData Load()
    {
        using var connection = Open(); using var transaction = connection.BeginTransaction(deferred: true);
        var data = Read(connection, transaction); transaction.Commit(); return data;
    }

    public InventoryData Update(Action<InventoryData> action)
    {
        using var connection = Open(); using var transaction = connection.BeginTransaction(deferred: false);
        var before = Read(connection, transaction);
        var next = new InventoryData { Products = before.Products.Select(p => p.Copy()).ToList(), Movements = before.Movements.ToList() };
        action(next);
        WriteChanges(connection, transaction, before, next);
        transaction.Commit();
        return next;
    }

    private static InventoryData Read(SqliteConnection connection, SqliteTransaction transaction)
    {
        var data = new InventoryData();
        using(var revision = connection.CreateCommand())
        {
            revision.Transaction=transaction; revision.CommandText="SELECT Value FROM InventoryMetadata WHERE Key='revision'";
            data.Revision=revision.ExecuteScalar() as string ?? "initial";
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT Id,Name,Barcode,Sku,Category,Cost,Price,OldPrice,Stock,Minimum,Maximum,Unit,Description,ImagePath,OnMenu,Active,CreatedAt,UpdatedAt,SourceFields,ImageData,SourceImage FROM Products ORDER BY Name";
            using var r = command.ExecuteReader();
            while (r.Read()) data.Products.Add(new Product { Id=Guid.Parse(r.GetString(0)),Name=r.GetString(1),Barcode=r.GetString(2),Sku=r.GetString(3),Category=r.GetString(4),Cost=Decimal(r,5),Price=Decimal(r,6),OldPrice=r.IsDBNull(7)?null:Decimal(r,7),Stock=Decimal(r,8),Minimum=Decimal(r,9),Maximum=Decimal(r,10),Unit=r.GetString(11),Description=r.GetString(12),ImagePath=r.GetString(13),OnMenu=r.GetInt64(14)!=0,Active=r.GetInt64(15)!=0,CreatedAt=DateTime.Parse(r.GetString(16),null,DateTimeStyles.RoundtripKind),UpdatedAt=DateTime.Parse(r.GetString(17),null,DateTimeStyles.RoundtripKind),SourceFields=JsonSerializer.Deserialize<Dictionary<string,string?>>(r.GetString(18))??[],ImageData=r.IsDBNull(19)?null:(byte[])r.GetValue(19),SourceImage=r.GetString(20) });
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT Id,ProductId,ProductName,Barcode,Kind,BeforeAmount,Delta,AfterAmount,Note,At FROM Movements ORDER BY At, rowid";
            using var r = command.ExecuteReader();
            while (r.Read()) data.Movements.Add(new Movement { Id=Guid.Parse(r.GetString(0)),ProductId=Guid.Parse(r.GetString(1)),ProductName=r.GetString(2),Barcode=r.GetString(3),Kind=r.GetString(4),Before=Decimal(r,5),Delta=Decimal(r,6),After=Decimal(r,7),Note=r.GetString(8),At=DateTime.Parse(r.GetString(9),null,DateTimeStyles.RoundtripKind) });
        }
        return data;
    }

    private static void WriteChanges(SqliteConnection connection, SqliteTransaction transaction, InventoryData before, InventoryData next)
    {
        var oldProducts = before.Products.ToDictionary(p => p.Id);
        var retained = next.Products.Select(p => p.Id).ToHashSet();
        foreach (var deleted in before.Products.Where(p => !retained.Contains(p.Id)))
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "DELETE FROM Products WHERE Id=$id"; Add(command,"$id",deleted.Id.ToString()); command.ExecuteNonQuery();
        }
        foreach (var p in next.Products)
        {
            // Compare only changed rows; never rewrite the stock movement history.
            if (oldProducts.TryGetValue(p.Id, out var old) && SameProduct(old, p)) continue;
            using var c = connection.CreateCommand(); c.Transaction = transaction;
            c.CommandText = """
                INSERT INTO Products(Id,Name,Barcode,Sku,Category,Cost,Price,OldPrice,Stock,Minimum,Maximum,Unit,Description,ImagePath,OnMenu,Active,CreatedAt,UpdatedAt,SourceFields,ImageData,SourceImage)
                VALUES($Id,$Name,$Barcode,$Sku,$Category,$Cost,$Price,$OldPrice,$Stock,$Minimum,$Maximum,$Unit,$Description,$ImagePath,$OnMenu,$Active,$CreatedAt,$UpdatedAt,$SourceFields,$ImageData,$SourceImage)
                ON CONFLICT(Id) DO UPDATE SET Name=excluded.Name,Barcode=excluded.Barcode,Sku=excluded.Sku,Category=excluded.Category,Cost=excluded.Cost,Price=excluded.Price,OldPrice=excluded.OldPrice,Stock=excluded.Stock,Minimum=excluded.Minimum,Maximum=excluded.Maximum,Unit=excluded.Unit,Description=excluded.Description,ImagePath=excluded.ImagePath,OnMenu=excluded.OnMenu,Active=excluded.Active,CreatedAt=excluded.CreatedAt,UpdatedAt=excluded.UpdatedAt,SourceFields=excluded.SourceFields,ImageData=excluded.ImageData,SourceImage=excluded.SourceImage
                """;
            Add(c,"$Id",p.Id.ToString());Add(c,"$Name",p.Name);Add(c,"$Barcode",p.Barcode);Add(c,"$Sku",p.Sku);Add(c,"$Category",p.Category);Add(c,"$Cost",Number(p.Cost));Add(c,"$Price",Number(p.Price));Add(c,"$OldPrice",p.OldPrice.HasValue?Number(p.OldPrice.Value):DBNull.Value);Add(c,"$Stock",Number(p.Stock));Add(c,"$Minimum",Number(p.Minimum));Add(c,"$Maximum",Number(p.Maximum));Add(c,"$Unit",p.Unit);Add(c,"$Description",p.Description);Add(c,"$ImagePath",p.ImagePath);Add(c,"$OnMenu",p.OnMenu?1:0);Add(c,"$Active",p.Active?1:0);Add(c,"$CreatedAt",p.CreatedAt.ToString("O"));Add(c,"$UpdatedAt",p.UpdatedAt.ToString("O"));Add(c,"$SourceFields",JsonSerializer.Serialize(p.SourceFields));Add(c,"$ImageData",p.ImageData is {Length:>0}?p.ImageData:DBNull.Value);Add(c,"$SourceImage",p.SourceImage);c.ExecuteNonQuery();
        }
        var existing = before.Movements.Select(m => m.Id).ToHashSet();
        foreach (var m in next.Movements.Where(m => !existing.Contains(m.Id)))
        {
            using var c = connection.CreateCommand(); c.Transaction = transaction;
            c.CommandText = "INSERT INTO Movements VALUES($Id,$ProductId,$ProductName,$Barcode,$Kind,$Before,$Delta,$After,$Note,$At)";
            Add(c,"$Id",m.Id.ToString());Add(c,"$ProductId",m.ProductId.ToString());Add(c,"$ProductName",m.ProductName);Add(c,"$Barcode",m.Barcode);Add(c,"$Kind",m.Kind);Add(c,"$Before",Number(m.Before));Add(c,"$Delta",Number(m.Delta));Add(c,"$After",Number(m.After));Add(c,"$Note",m.Note);Add(c,"$At",m.At.ToString("O"));c.ExecuteNonQuery();
        }
        next.Revision=Guid.NewGuid().ToString("N");
        using var revision=connection.CreateCommand();revision.Transaction=transaction;
        revision.CommandText="INSERT INTO InventoryMetadata(Key,Value) VALUES('revision',$value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value";
        Add(revision,"$value",next.Revision);revision.ExecuteNonQuery();
    }

    public void BackupTo(string destination)
    {
        using var source = Open(); using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource=destination, Pooling=false }.ConnectionString);
        target.Open(); source.BackupDatabase(target);
    }
    private static readonly System.Reflection.PropertyInfo[] ScalarFields = typeof(Product).GetProperties().Where(p => p.Name is not (nameof(Product.ImageData) or nameof(Product.SourceFields))).ToArray();
    private static bool SameProduct(Product a, Product b) => ScalarFields.All(p => Equals(p.GetValue(a), p.GetValue(b)))
        && (a.ImageData ?? []).AsSpan().SequenceEqual(b.ImageData ?? [])
        && a.SourceFields.Count == b.SourceFields.Count && a.SourceFields.All(pair => b.SourceFields.TryGetValue(pair.Key, out var value) && pair.Value == value);
    private static decimal Decimal(SqliteDataReader r,int index)=>decimal.Parse(r.GetString(index),CultureInfo.InvariantCulture);
    private static string Number(decimal value)=>value.ToString(CultureInfo.InvariantCulture);
    private static void Add(SqliteCommand command,string name,object value)=>command.Parameters.AddWithValue(name,value);
}
