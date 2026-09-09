using System.Drawing.Printing;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Windows;
namespace BarcodePrinter.Services.Printing;
public sealed class PrinterSettingsService
{
    private readonly string path = Path.Combine(JsonStore.Root,"printers.json");
    public List<PrinterProfile> Profiles() => File.Exists(path) ? JsonStore.Read<List<PrinterProfile>>(path) : [];
    public PrinterProfile Get(string name) => PrinterRouting.Resolve(Profiles().FirstOrDefault(p=>p.PrinterName==name) ?? new PrinterProfile { PrinterName=name });
    public void Save(PrinterProfile profile) { profile.Calibration.Validate(); if (!new[] {203,300,600}.Contains(profile.Dpi)) throw new InvalidOperationException("DPI 203, 300 veya 600 olmalıdır."); var items=Profiles(); items.RemoveAll(p=>p.PrinterName==profile.PrinterName);items.Insert(0,profile);JsonStore.Save(path,items); }
    public Task<List<PrinterInfo>> DiscoverAsync() => Task.Run(() =>
    {
        var result=new List<PrinterInfo>();
        foreach(string name in PrinterSettings.InstalledPrinters)
        {
            try
            {
                var detail=WindowsPrinterInterop.Info(name); var settings=new PrinterSettings { PrinterName=name };
                var status=(detail.Status & 0x80)!=0 ? "Çevrimdışı" : detail.Status==0 ? "Hazır (Windows bildirimi)" : $"Windows durum kodu: {detail.Status}";
                var resolutions=string.Join(", ",settings.PrinterResolutions.Cast<PrinterResolution>().Where(r=>r.X>0).Select(r=>$"{r.X}×{r.Y}").Distinct());
                result.Add(new(name,detail.DriverName??"",detail.PortName??"",status,settings.IsDefaultPrinter,settings.DefaultPageSettings.PaperSize.PaperName,resolutions,name.Contains("TSC",StringComparison.OrdinalIgnoreCase)||(detail.DriverName??"").Contains("TSC",StringComparison.OrdinalIgnoreCase)));
            } catch(Exception ex) { JsonStore.Log(ex); result.Add(new(name,"","","Bilgi alınamadı: "+ex.Message,false,"","",name.Contains("TSC",StringComparison.OrdinalIgnoreCase))); }
        }
        return result;
    });
}
