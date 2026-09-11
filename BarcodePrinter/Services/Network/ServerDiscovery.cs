using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace BarcodePrinter.Services.Network;

public sealed record DiscoveryEndpoint(int Port);
public sealed record DiscoveredServer(string Name, string Url)
{
    public override string ToString() => $"{Name} · {Url}";
}

public sealed class ServerDiscoveryResponder(DiscoveryEndpoint endpoint) : BackgroundService
{
    internal const string Request = "BARCODEPRO_DISCOVER_V1";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, ServerConfiguration.DiscoveryPort));
            while (!stoppingToken.IsCancellationRequested)
            {
                var packet = await udp.ReceiveAsync(stoppingToken);
                if (packet.Buffer.Length > 64 || Encoding.ASCII.GetString(packet.Buffer) != Request) continue;
                // Discovery advertises only an endpoint. Credentials and inventory require authenticated HTTP requests.
                var response = JsonSerializer.SerializeToUtf8Bytes(new { protocol = Request, name = Environment.MachineName, port = endpoint.Port });
                await udp.SendAsync(response, packet.RemoteEndPoint, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (SocketException) { /* Manual address entry remains available if discovery cannot bind. */ }
    }
}

public static class ServerDiscovery
{
    public static async Task<IReadOnlyList<DiscoveredServer>> FindAsync(CancellationToken token = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0)) { EnableBroadcast = true };
        var bytes = Encoding.ASCII.GetBytes(ServerDiscoveryResponder.Request);
        await udp.SendAsync(bytes, new IPEndPoint(IPAddress.Broadcast, ServerConfiguration.DiscoveryPort), timeout.Token);
        var found = new Dictionary<string, DiscoveredServer>();
        try
        {
            while (!timeout.IsCancellationRequested)
            {
                var packet = await udp.ReceiveAsync(timeout.Token);
                if (packet.Buffer.Length > 1024) continue;
                try
                {
                    using var json = JsonDocument.Parse(packet.Buffer);
                    var root = json.RootElement;
                    if (!root.TryGetProperty("protocol", out var protocol) || protocol.GetString() != ServerDiscoveryResponder.Request ||
                        !root.TryGetProperty("port", out var port) || !port.TryGetInt32(out int portNumber) || portNumber is < 1024 or > 65535) continue;
                    var name = root.TryGetProperty("name", out var serverName) ? serverName.GetString() ?? "Server" : "Server";
                    var url = $"http://{packet.RemoteEndPoint.Address}:{portNumber}";
                    found[url] = new DiscoveredServer(name.Length > 80 ? name[..80] : name, url);
                }
                catch (JsonException) { }
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
        return found.Values.OrderBy(s => s.Name).ToArray();
    }
}
