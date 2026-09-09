using BarcodePrinter.Helpers;
using BarcodePrinter.Models.Printing;
namespace BarcodePrinter.Services.Printing;

public static class PrinterRouting
{
    public static bool IsTsc(string name,string driver="")=>name.Contains("TSC",StringComparison.OrdinalIgnoreCase)||driver.Contains("TSC",StringComparison.OrdinalIgnoreCase)||Is244Ce(name)||Is244Ce(driver);
    private static bool Is244Ce(string value)=>new string(value.Where(char.IsLetterOrDigit).ToArray()).Contains("TTP244CE",StringComparison.OrdinalIgnoreCase);
    public static PrinterProfile Resolve(PrinterProfile input,string driver="")
    {
        var profile=JsonStore.Clone(input);
        if(IsTsc(profile.PrinterName,driver)||profile.Model==Ttp244CePrinter.Model)profile.Mode=PrintMode.RawTspl;
        if(Is244Ce(profile.PrinterName)||Is244Ce(driver)||profile.Model==Ttp244CePrinter.Model){profile.Model=Ttp244CePrinter.Model;profile.Dpi=203;}
        return profile;
    }
    public static void ValidatePort(string printerName,string? port)
    {
        if(string.IsNullOrWhiteSpace(port))throw new InvalidOperationException("Yazıcının bağlantı noktası okunamadı. Windows yazıcı bağlantısını kontrol edin.");
        foreach(var entry in port.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
        {
            var normalized=entry.TrimEnd(':').ToUpperInvariant();
            if(normalized is "FILE" or "PORTPROMPT" || entry.Contains(":\\",StringComparison.Ordinal) || entry.StartsWith('/'))
                throw new InvalidOperationException($"{printerName}: '{port}' dosyaya çıktı portudur. Windows > Yazıcı özellikleri > Bağlantı Noktaları bölümünde cihazın gerçek USB veya ağ portunu seçin. PRN dosyası oluşturulmadı.");
        }
    }
}
