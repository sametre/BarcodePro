using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using BarcodePrinter.Helpers;

namespace BarcodePrinter.Services.Network;

public sealed class ServerConfiguration
{
    public const string ServiceName = "BarcodeProServer";
    public const int DiscoveryPort = 5089;
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BarcodePro", "Server");
    public static string InventoryPath => Path.Combine(DataDirectory, "inventory.db");
    public int Port { get; set; } = 5088;
    public string AccessKey { get; set; } = "";

    public static ServerConfiguration Load(string? directory = null, bool create = false)
    {
        var path = Path.Combine(directory ?? DataDirectory, "server.json");
        if (!File.Exists(path) && !create)
            throw new InvalidOperationException("Server kurulumu bulunamadı. Server Setup.exe dosyasını yönetici olarak çalıştırın.");
        var settings = File.Exists(path) ? JsonStore.Read<ServerConfiguration>(path) : new ServerConfiguration();
        if (settings.Port is < 1024 or > 65535) throw new InvalidDataException("Server portu 1024–65535 arasında olmalıdır.");
        if (string.IsNullOrWhiteSpace(settings.AccessKey))
        {
            if (!create) throw new InvalidDataException("Server erişim anahtarı eksik. Server kurulumunu onarın.");
            settings.AccessKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            JsonStore.Save(path, settings);
        }
        return settings;
    }

    public NetworkSettings LocalClient() => new() { ServerUrl = $"http://127.0.0.1:{Port}", AccessKey = AccessKey };
    public void Save(string? directory=null) => JsonStore.Save(Path.Combine(directory??DataDirectory,"server.json"),this);

    public static string[] LocalAddresses() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Select(n => n.Address)
        .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a) && !a.ToString().StartsWith("169.254.", StringComparison.Ordinal))
        .Select(a => a.ToString()).Distinct().OrderBy(a => a).ToArray();

    public static async Task<string> PublicAddressAsync(CancellationToken token = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var value = (await http.GetStringAsync("https://api.ipify.org", token)).Trim();
            return IPAddress.TryParse(value, out var ip) ? ip.ToString() : "Alınamadı";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return "Alınamadı (internet bağlantısını kontrol edin)"; }
    }
}
