using BarcodePrinter.Helpers;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Windows;
using System.ComponentModel;
using System.Diagnostics;
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
    public static bool IsFilePort(string? port)
    {
        if(string.IsNullOrWhiteSpace(port))return true;
        return port.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Any(entry=>entry.TrimEnd(':').Equals("FILE",StringComparison.OrdinalIgnoreCase)||entry.TrimEnd(':').Equals("PORTPROMPT",StringComparison.OrdinalIgnoreCase)||entry.Contains(":\\",StringComparison.Ordinal)||entry.StartsWith('/'));
    }
    public static IReadOnlyList<string> AutomaticCandidates(IEnumerable<string> ports)
    {
        var usable=ports.Where(p=>!IsFilePort(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var usb=usable.Where(IsUsbPort).OrderBy(p=>UsbPortRank(p)).ToList();if(usb.Count>0)return usb;
        return usable.Where(p=>p.StartsWith("WSD-",StringComparison.OrdinalIgnoreCase)||p.StartsWith("IP_",StringComparison.OrdinalIgnoreCase)).ToList();
    }
    public static bool IsUsbPort(string port) => System.Text.RegularExpressions.Regex.IsMatch(port.Trim(), "^USB0*\\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    private static int UsbPortRank(string port)
    {
        var digits=new string(port.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits,out var n)) return n == 2 ? 0 : n + 1;
        return int.MaxValue;
    }
    public static string EnsurePhysicalPort(string printerName,string? currentPort)
    {
        if(!IsFilePort(currentPort)){ValidatePort(printerName,currentPort);return currentPort!;}
        var candidates=UnassignedCandidates(printerName);
        if(candidates.Count==0)throw new InvalidOperationException($"{printerName}: Windows'ta kullanılabilir USB veya ağ yazıcı portu bulunamadı. TSC USB kablosunu takın ve yazıcı sürücüsünü kurun.");
        if(candidates.Count>1)throw new InvalidOperationException($"{printerName}: Birden fazla yazıcı portu bulundu ({string.Join(", ",candidates)}). Yanlış cihaza baskı göndermemek için Yazıcılar ekranından portu seçin.");
        AssignPhysicalPort(printerName,candidates[0]);return candidates[0];
    }
    public static IReadOnlyList<string> UnassignedCandidates(string printerName)
    {
        var assigned=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(string queue in System.Drawing.Printing.PrinterSettings.InstalledPrinters)if(!queue.Equals(printerName,StringComparison.OrdinalIgnoreCase))try{var port=WindowsPrinterInterop.Info(queue).PortName;if(!IsFilePort(port)&&port!=null)foreach(var item in port.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))assigned.Add(item);}catch(System.ComponentModel.Win32Exception){}
        return AutomaticCandidates(WindowsPrinterInterop.Ports()).Where(p=>!assigned.Contains(p)).ToList();
    }
    public static void AssignPhysicalPort(string printerName,string port)
    {
        if(!AutomaticCandidates([port]).Contains(port,StringComparer.OrdinalIgnoreCase))throw new InvalidOperationException("Yalnızca gerçek USB, WSD veya IP yazıcı portu seçilebilir.");
        try{WindowsPrinterInterop.SetPort(printerName,port);}
        catch(Win32Exception ex)when(ex.NativeErrorCode==5&&!IsElevatedPortHelper())
        {
            var executable=Environment.ProcessPath??throw new InvalidOperationException("Uygulama yolu bulunamadı.");
            var start=new ProcessStartInfo(executable){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden};start.ArgumentList.Add("--set-printer-port");start.ArgumentList.Add(printerName);start.ArgumentList.Add(port);
            using var process=Process.Start(start)??throw new InvalidOperationException("Yazıcı portu için yönetici işlemi başlatılamadı.");
            if(!process.WaitForExit(30000)){try{process.Kill();}catch(InvalidOperationException){}throw new InvalidOperationException("Yazıcı portu ayarı zaman aşımına uğradı.");}
            if(process.ExitCode!=0)throw new InvalidOperationException("Windows yazıcı portu değiştirilemedi. Yönetici onayını ve yazıcı izinlerini kontrol edin.");
        }
        ValidatePort(printerName,WindowsPrinterInterop.Info(printerName).PortName);
    }
    internal static bool IsElevatedPortHelper()=>Environment.GetCommandLineArgs().Skip(1).FirstOrDefault()=="--set-printer-port";
    internal static int RunPortHelper(string[] args)
    {
        if(args.Length!=3||args[0]!="--set-printer-port")return 2;
        try{if(!AutomaticCandidates([args[2]]).Contains(args[2],StringComparer.OrdinalIgnoreCase))return 3;WindowsPrinterInterop.SetPort(args[1],args[2]);return WindowsPrinterInterop.Info(args[1]).PortName?.Equals(args[2],StringComparison.OrdinalIgnoreCase)==true?0:4;}catch{return 5;}
    }
}
