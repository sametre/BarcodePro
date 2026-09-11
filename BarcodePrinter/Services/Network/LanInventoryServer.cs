using BarcodePrinter.Helpers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BarcodePrinter.Services.Network;

public sealed class LanInventoryServer : IAsyncDisposable
{
    private readonly WebApplication app;
    public int Port { get; }
    private LanInventoryServer(WebApplication app,int port){this.app=app;Port=port;}
    public static async Task<LanInventoryServer> StartAsync(int port=5088,string? inventoryPath=null,string? accessKey=null,CancellationToken token=default)
    {
        var app=Build(port,inventoryPath??ServerConfiguration.InventoryPath,accessKey);
        try { await app.StartAsync(token);return new LanInventoryServer(app,port); }
        catch { await app.DisposeAsync();throw; }
    }
    public static async Task RunServiceAsync(string? directory=null,string serviceName=ServerConfiguration.ServiceName)
    {
        var settings=ServerConfiguration.Load(directory);
        await using var app=Build(settings.Port,Path.Combine(directory??ServerConfiguration.DataDirectory,"inventory.db"),settings.AccessKey,serviceName);
        await app.RunAsync();
    }
    private static WebApplication Build(int port,string inventoryPath,string? accessKey,string? serviceName=null)
    {
        if(string.IsNullOrWhiteSpace(accessKey))throw new InvalidOperationException("Server erişim anahtarı boş olamaz.");
        var builder=WebApplication.CreateSlimBuilder(new WebApplicationOptions{ContentRootPath=AppContext.BaseDirectory});
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        if(serviceName!=null)
        {
            builder.Services.AddWindowsService(options=>options.ServiceName=serviceName);
            builder.Services.AddSingleton(new DiscoveryEndpoint(port));
            builder.Services.AddHostedService<ServerDiscoveryResponder>();
        }
        var app=builder.Build();var inventory=new Inventory(inventoryPath);var gate=new object();
        var keyBytes=Encoding.UTF8.GetBytes(accessKey);
        bool Authorized(HttpContext c)=>c.Request.Headers.TryGetValue("X-BarcodePro-Key",out var key)&&CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(key.ToString()),keyBytes);
        IResult Execute(HttpContext c,Action action)
        {
            if(!Authorized(c))return Results.Unauthorized();
            try{lock(gate){action();return Results.Json(JsonStore.Clone(inventory.Data));}}catch(InvalidOperationException ex){return Results.BadRequest(ex.Message);}
        }
        app.MapGet("/api/health",(HttpContext c)=>Authorized(c)?Results.Ok(new{status="ok",server="Barcode Pro Server"}):Results.Unauthorized());
        app.MapGet("/api/inventory",(HttpContext c)=>
        {
            if(!Authorized(c))return Results.Unauthorized();
            lock(gate)
            {
                var revision=inventory.Revision;
                if(c.Request.Headers.IfNoneMatch.ToString()=="\""+revision+"\"")return Results.StatusCode(StatusCodes.Status304NotModified);
                inventory.Refresh();
                c.Response.Headers.ETag="\""+inventory.Data.Revision+"\"";
                return Results.Json(JsonStore.Clone(inventory.Data));
            }
        });
        app.MapPost("/api/products",(HttpContext c,SaveProductRequest r)=>Execute(c,()=>inventory.SaveProduct(r.Product)));
        app.MapPost("/api/stock",(HttpContext c,MoveStockRequest r)=>Execute(c,()=>inventory.Move(r.ProductId,r.Kind,r.Quantity,r.Note)));
        app.MapPost("/api/products/delete",(HttpContext c,DeleteProductRequest r)=>Execute(c,()=>inventory.Delete(r.ProductId)));
        app.MapPost("/api/seed",(HttpContext c,SeedRequest r)=>Execute(c,()=>{if(r.Confirm)inventory.SeedDemo();}));
        return app;
    }
    public async ValueTask DisposeAsync(){await app.StopAsync();await app.DisposeAsync();}
}
