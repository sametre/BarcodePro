using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BarcodePrinter.Services.Licensing;

public sealed record LicenseInfo(string Key, DateTimeOffset ExpiresAtUtc, string Product = "R3 M-Kobi", string Edition = "Server and Client");

/// <summary>Local, machine-scoped license file used by both the Server service and Client.</summary>
public static class LicenseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public static string Root(bool server) => server ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "R3-M-Kobi") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "R3-M-Kobi");
    public static string PathFor(bool server) => Path.Combine(Root(server), "license.json");
    public static LicenseInfo Create(int validDays)
    {
        if (validDays is < 1 or > 3650) throw new ArgumentOutOfRangeException(nameof(validDays));
        Span<byte> bytes = stackalloc byte[6]; RandomNumberGenerator.Fill(bytes);
        var key = string.Concat(bytes.ToArray().Select(b => Alphabet[b % Alphabet.Length]));
        return new LicenseInfo(key, DateTimeOffset.UtcNow.AddDays(validDays));
    }
    public static LicenseInfo? Read(bool server)
    {
        var path = PathFor(server); if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<LicenseInfo>(File.ReadAllText(path), JsonOptions); } catch (JsonException) { return null; } catch (IOException) { return null; }
    }
    public static string? Validate(bool server)
    {
        var info = Read(server); if (info is null) return "Lisans dosyası bulunamadı.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(info.Key ?? "", "^[A-Z0-9]{6}$")) return "Lisans anahtarı 6 karakter olmalıdır.";
        if (!info.Product.Equals("R3 M-Kobi", StringComparison.OrdinalIgnoreCase)) return "Lisans başka bir ürüne ait.";
        return info.ExpiresAtUtc <= DateTimeOffset.UtcNow ? $"Lisans süresi doldu ({info.ExpiresAtUtc:dd.MM.yyyy})." : null;
    }
    public static void Save(LicenseInfo info, bool server)
    {
        if (ValidateInfo(info) is string error) throw new InvalidDataException(error);
        var root = Root(server); Directory.CreateDirectory(root); var path = PathFor(server); var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(info, JsonOptions)); File.Move(temp, path, true);
    }
    public static bool Activate(string source, string enteredKey, bool server, out string message)
    {
        message = ""; LicenseInfo? info;
        try { info = JsonSerializer.Deserialize<LicenseInfo>(File.ReadAllText(source), JsonOptions); } catch (Exception ex) when (ex is IOException or JsonException) { message = "Lisans dosyası okunamadı."; return false; }
        if (info is null || !string.Equals(info.Key, enteredKey.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase)) { message = "Lisans numarası dosya ile eşleşmiyor."; return false; }
        if (ValidateInfo(info) is string error) { message = error; return false; }
        Save(info, server); message = $"Lisans etkinleştirildi. Bitiş: {info.ExpiresAtUtc:dd.MM.yyyy}"; return true;
    }
    private static string? ValidateInfo(LicenseInfo info) => string.IsNullOrWhiteSpace(info.Key) || !System.Text.RegularExpressions.Regex.IsMatch(info.Key, "^[A-Z0-9]{6}$") ? "Geçersiz 6 karakterli lisans numarası." : info.ExpiresAtUtc <= DateTimeOffset.UtcNow ? "Lisans süresi dolmuş." : !info.Product.Equals("R3 M-Kobi", StringComparison.OrdinalIgnoreCase) ? "Lisans ürünü geçersiz." : null;
}
