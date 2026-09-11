using System.Net;
using System.Net.Sockets;
using BarcodePrinter;
using BarcodePrinter.Services.Network;

internal static class NetworkChecks
{
    public static int Run(string directory)
    {
        int count=0;void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
        using var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
        var server=LanInventoryServer.StartAsync(port,Path.Combine(directory,"server-inventory.json"),"test-key").GetAwaiter().GetResult();
        try
        {
            using var client=new RemoteInventoryClient(new NetworkSettings{ServerUrl=$"http://127.0.0.1:{port}",AccessKey="test-key"});
            Check(client.Health(),"LAN server health and access key");
            var product=new Product{Name="Ağ ürünü",Barcode="8691234000001",Sku="NET-1",Stock=5};client.Save(product);
            Check(client.Snapshot().Products.Single().Stock==5,"Client saves product on server");
            client.Move(product.Id,"Stok çıkışı",2,"test");Check(client.Snapshot().Products.Single().Stock==3,"Client stock movement is centralized");
            var snapshot=client.Snapshot();
            Check(ReferenceEquals(snapshot,client.SnapshotIfChanged(snapshot)),"Unchanged remote refresh skips downloading inventory and images");
            var admin=new Inventory(Path.Combine(directory,"server-inventory.json"));
            var edited=admin.Data.Products.Single().Copy();edited.Name="Server yönetim değişikliği";edited.ImageData=[1,2,3];admin.SaveProduct(edited);
            var updated=client.SnapshotIfChanged(snapshot);
            Check(updated.Products.Single().Name==edited.Name && updated.Products.Single().ImageData!.SequenceEqual(edited.ImageData) && updated.Revision!=snapshot.Revision,"Clients detect independent Server admin writes and receive embedded images");
            try{using var denied=new RemoteInventoryClient(new NetworkSettings{ServerUrl=$"http://127.0.0.1:{port}",AccessKey="wrong"});denied.Snapshot();throw new Exception("Unauthorized client accepted");}catch(InvalidOperationException){Check(true,"LAN server rejects wrong access key");}
        }
        finally{server.DisposeAsync().AsTask().GetAwaiter().GetResult();}
        return count;
    }
}
