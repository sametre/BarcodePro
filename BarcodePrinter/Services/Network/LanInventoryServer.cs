using BarcodePrinter.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace BarcodePrinter.Services.Network;

public sealed class LanInventoryServer : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly object gate=new();
    public int Port { get; }
    private LanInventoryServer(WebApplication app,int port){this.app=app;Port=port;}
    public static async Task<LanInventoryServer> StartAsync(int port=5088,string? inventoryPath=null,string accessKey="owner",CancellationToken token=default)
    {
        var builder=WebApplication.CreateSlimBuilder();builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        var app=builder.Build();var inventory=new Inventory(inventoryPath??Path.Combine(JsonStore.Root,"inventory.json"));var gate=new object();
        bool Authorized(HttpContext c)=>c.Request.Headers.TryGetValue("X-BarcodePro-Key",out var key)&&key==accessKey;
        IResult Execute(HttpContext c,Action action)
        {
            if(!Authorized(c))return Results.Unauthorized();
            try{lock(gate)action();return Results.Json(inventory.Data);}catch(InvalidOperationException ex){return Results.BadRequest(ex.Message);}
        }
        app.MapGet("/api/health",(HttpContext c)=>Authorized(c)?Results.Ok(new{status="ok",server="Barcode Pro Server"}):Results.Unauthorized());
        app.MapGet("/api/inventory",(HttpContext c)=>{if(!Authorized(c))return Results.Unauthorized();lock(gate)return Results.Json(inventory.Data);});
        app.MapPost("/api/products",(HttpContext c,SaveProductRequest r)=>Execute(c,()=>inventory.SaveProduct(r.Product)));
        app.MapPost("/api/stock",(HttpContext c,MoveStockRequest r)=>Execute(c,()=>inventory.Move(r.ProductId,r.Kind,r.Quantity,r.Note)));
        app.MapPost("/api/products/delete",(HttpContext c,DeleteProductRequest r)=>Execute(c,()=>inventory.Delete(r.ProductId)));
        app.MapPost("/api/seed",(HttpContext c,SeedRequest r)=>Execute(c,()=>{if(r.Confirm)inventory.SeedDemo();}));
        await app.StartAsync(token);return new LanInventoryServer(app,port);
    }
    public async ValueTask DisposeAsync(){await app.StopAsync();await app.DisposeAsync();}
}
