using System.Net.Http.Json;
using System.Text.Json;
using BarcodePrinter.Helpers;

namespace BarcodePrinter.Services.Network;

public sealed class NetworkSettings
{
    public string ServerUrl { get; set; } = "http://127.0.0.1:5088";
    public string AccessKey { get; set; } = "";
    public static string PathName => Path.Combine(JsonStore.Root,"network.json");
    public static NetworkSettings Load() => File.Exists(PathName) ? JsonStore.Read<NetworkSettings>(PathName) : new();
    public void Save() => JsonStore.Save(PathName,this);
    public Uri BaseUri()
    {
        if(!Uri.TryCreate(ServerUrl.Trim().TrimEnd('/')+"/",UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Geçerli bir server adresi girin. Örnek: http://192.168.1.20:5088");
        return uri;
    }
}

public sealed record SaveProductRequest(Product Product);
public sealed record MoveStockRequest(Guid ProductId,string Kind,decimal Quantity,string Note);
public sealed record DeleteProductRequest(Guid ProductId);
public sealed record SeedRequest(bool Confirm);

public sealed class RemoteInventoryClient : IDisposable
{
    private readonly HttpClient http;
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web){PropertyNameCaseInsensitive=true};
    public RemoteInventoryClient(NetworkSettings settings)
    {
        http=new HttpClient{BaseAddress=settings.BaseUri(),Timeout=TimeSpan.FromSeconds(20)};
        http.DefaultRequestHeaders.Add("X-BarcodePro-Key",settings.AccessKey);
    }
    public InventoryData Snapshot()=>Send<InventoryData>(HttpMethod.Get,"api/inventory",null);
    public InventoryData SnapshotIfChanged(InventoryData current)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,"api/inventory");
        if(!string.IsNullOrWhiteSpace(current.Revision))request.Headers.TryAddWithoutValidation("If-None-Match","\""+current.Revision+"\"");
        using var response=http.Send(request);
        if(response.StatusCode==System.Net.HttpStatusCode.NotModified)return current;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadFromJsonAsync<InventoryData>(JsonOptions).GetAwaiter().GetResult()??throw new InvalidOperationException("Server boş yanıt verdi.");
    }
    public InventoryData Save(Product product)=>Send<InventoryData>(HttpMethod.Post,"api/products",new SaveProductRequest(product));
    public InventoryData Move(Guid id,string kind,decimal quantity,string note)=>Send<InventoryData>(HttpMethod.Post,"api/stock",new MoveStockRequest(id,kind,quantity,note));
    public InventoryData Delete(Guid id)=>Send<InventoryData>(HttpMethod.Post,"api/products/delete",new DeleteProductRequest(id));
    public InventoryData Seed()=>Send<InventoryData>(HttpMethod.Post,"api/seed",new SeedRequest(true));
    public bool Health()
    {
        using var response=http.GetAsync("api/health").GetAwaiter().GetResult();
        return response.IsSuccessStatusCode;
    }
    private T Send<T>(HttpMethod method,string path,object? body)
    {
        using var request=new HttpRequestMessage(method,path);
        if(body!=null)request.Content=JsonContent.Create(body,options:JsonOptions);
        using var response=http.Send(request);
        if(!response.IsSuccessStatusCode)
        {
            var detail=response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)?$"Server işlemi başarısız: {(int)response.StatusCode}":detail.Trim('"'));
        }
        return response.Content.ReadFromJsonAsync<T>(JsonOptions).GetAwaiter().GetResult()??throw new InvalidOperationException("Server boş yanıt verdi.");
    }
    public void Dispose()=>http.Dispose();
}
