using BarcodePrinter;
using System.Globalization;

internal static class ConnectionChecks
{
    public static int Run(string directory)
    {
        int count=0;
        void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
        void Reject(Action action,string name){try{action();}catch(InvalidOperationException){Check(true,name);return;}throw new Exception(name);}
        var settings=new MySqlSourceSettings{Host="localhost",Database="test",Username="reader"};settings.SetPassword("test-only-password");
        Check(settings.SslMode==MySqlConnector.MySqlSslMode.Required&&MySqlProductSource.BuildConnectionString(settings).Contains("SSL Mode=Required"),"Default MySQL connection requires encrypted hosting-compatible TLS");
        settings.SslMode=MySqlConnector.MySqlSslMode.VerifyFull;Check(MySqlProductSource.BuildConnectionString(settings).Contains("SSL Mode=VerifyFull"),"Strict certificate verification remains available");settings.SslMode=MySqlConnector.MySqlSslMode.Required;
        Check(MySqlProductSource.FriendlyError(1042,"Unable to connect",settings).Contains("TLS hatası değildir")&&MySqlProductSource.FriendlyError(1042,"",settings).Contains("localhost:3306"),"MySQL 1042 diagnostic identifies network and host permission causes");
        Check(MySqlProductSource.FriendlyError(0,"certificate verify failed",settings).Contains("TLS bağlantısı"),"TLS certificate failures receive specific guidance");
        var settingsPath=Path.Combine(directory,"connection.json");settings.Save(settingsPath);
        Check(!File.ReadAllText(settingsPath).Contains("test-only-password")&&MySqlSourceSettings.Load(settingsPath).GetPassword()=="test-only-password","MySQL password encrypted and recoverable by Windows user");
        Check(MySqlProductSource.BuildSelect(settings,true).EndsWith("LIMIT 0"),"Connection test validates mapped table without loading products");
        settings.Table="products; DROP TABLE products";Reject(()=>MySqlProductSource.BuildSelect(settings),"SQL table injection rejected");settings.Table="products";
        settings.Columns["Name"]="name` FROM users --";Reject(()=>MySqlProductSource.BuildSelect(settings),"SQL column injection rejected");settings.Columns["Name"]="name";
        var culture=CultureInfo.CurrentCulture;
        Product product;
        try{CultureInfo.CurrentCulture=new CultureInfo("tr-TR");product=MySqlProductSource.MapRow(new Dictionary<string,object?>{{"Name","Test"},{"Barcode","00001234"},{"Sku","SKU-1"},{"Stock","12.5"},{"Price","9.95"},{"Active",1},{"OnMenu",false}});}
        finally{CultureInfo.CurrentCulture=culture;}
        Check(product.Price==9.95m&&product.Stock==12.5m&&product.Barcode=="00001234"&&product.Active&&!product.OnMenu,"MySQL decimals, booleans and barcode zeros mapped correctly");
        var inventoryPath=Path.Combine(directory,"import.json");var inventory=new Inventory(inventoryPath);string[] fields=["Name","Barcode","Sku","Stock","Price"];
        var first=inventory.ImportProducts([product],fields,false);Check(first.Added==1&&inventory.Data.Movements.Single().After==12.5m,"Initial MySQL stock imported with audit");
        var id=inventory.Data.Products.Single().Id;product.Stock=3;product.Price=11;
        inventory.ImportProducts([product],fields,false);Check(inventory.Data.Products.Single().Stock==12.5m&&inventory.Data.Products.Single().Price==11&&inventory.Data.Products.Single().Id==id,"Repeat import preserves ID and local stock by default");
        inventory.ImportProducts([product],fields,true);Check(inventory.Data.Products.Single().Stock==3&&inventory.Data.Movements.Last().Delta==-9.5m,"Explicit server stock refresh audited");
        var beforeStock=inventory.Data.Products.Single().Stock;var beforeMovements=inventory.Data.Movements.Count;var bad=product.Copy();bad.Barcode="99998888";bad.Sku="SECOND";bad.Price=-1;
        Reject(()=>inventory.ImportProducts([new Product{Name="Valid first",Barcode="77776666",Sku="FIRST"},bad],fields,false),"Invalid later row rejects entire import");var reopened=new Inventory(inventoryPath);Check(reopened.Data.Products.Count==1&&reopened.Data.Products.Single().Stock==beforeStock&&reopened.Data.Movements.Count==beforeMovements,"Rejected import leaves SQLite transaction unchanged");
        Reject(()=>inventory.ImportProducts([product,product],fields,false),"Duplicate source identities rejected");
        var other=new Product{Name="Second",Barcode="88889999",Sku="SECOND"};inventory.ImportProducts([other],fields,false);var conflict=product.Copy();conflict.Sku=other.Sku;
        Reject(()=>inventory.ImportProducts([conflict],fields,false),"Cross product barcode and SKU conflict rejected");
        var minimal=product.Copy();minimal.Price=0;inventory.ImportProducts([minimal],["Name","Barcode","Sku"],false);Check(inventory.Data.Products.Single(p=>p.Id==id).Price==11,"Unmapped fields preserved on existing products");
        return count;
    }
}
