using System.Text.Json;
using System.Text.Json.Serialization;
namespace BarcodePrinter.Helpers;
public static class JsonStore
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BarcodePro");
    public static JsonSerializerOptions Options { get; } = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;
    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { JsonSerializer.Serialize(stream, value, Options); stream.Flush(true); }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
    }
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Dosya boş veya geçersiz.");
    public static void Log(Exception exception) { try { var folder = Path.Combine(Root,"logs"); Directory.CreateDirectory(folder); File.AppendAllText(Path.Combine(folder, DateTime.Today.ToString("yyyyMMdd") + ".log"), DateTime.Now.ToString("O") + " " + exception + Environment.NewLine); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
