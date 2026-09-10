using BarcodePrinter.Helpers;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Services.Validation;
using BarcodePrinter.Printing.TSPL;
using BarcodePrinter.Printing.Windows;
namespace BarcodePrinter.Services.Printing;
public sealed class PrintQueueService
{
    public static PrintQueueService Instance {get;}=new();
    private readonly SemaphoreSlim gate=new(1,1);private readonly object sync=new();private readonly string path=Path.Combine(JsonStore.Root,"print-queue.json");
    private List<PrintJob> jobs=[];
    public event Action? Changed;
    private PrintQueueService()
    {
        try {if(File.Exists(path))jobs=JsonStore.Read<List<PrintJob>>(path);foreach(var job in jobs.Where(j=>j.Status is PrintJobStatus.Queued or PrintJobStatus.Printing)){job.Status=PrintJobStatus.Failed;job.Detail="Uygulama kapandı. Baskı teslimi belirsiz; tekrar basmadan yazıcıyı kontrol edin.";}}
        catch(Exception ex){JsonStore.Log(ex);}
    }
    public List<PrintJob> List(){lock(sync)return JsonStore.Clone(jobs);}
    private void Save(){JsonStore.Save(path,jobs);}
    public async Task EnqueueAsync(PrintJob input)
    {
        var job=JsonStore.Clone(input);job.Id=Guid.NewGuid();job.Time=DateTime.Now;job.Status=PrintJobStatus.Queued;
        if(string.IsNullOrWhiteSpace(job.Printer.PrinterName))throw new InvalidOperationException("Lütfen yazıcı seçin.");
        var target=WindowsPrinterInterop.Info(job.Printer.PrinterName);
        job.Printer=PrinterRouting.Resolve(job.Printer,target.DriverName??"");
        if(job.Printer.Mode==PrintMode.RawTspl)PrinterRouting.EnsurePhysicalPort(job.Printer.PrinterName,target.PortName);
        ValidationService.Template(job.Template);job.Printer.Calibration.Validate();
        Ttp244CePrinter.Validate(job.Template,job.Printer);
        if(job.Items.Count==0||job.Items.Any(i=>i.Quantity<1)||job.Items.Sum(i=>(long)i.Quantity)>10000)throw new InvalidOperationException("Toplam etiket adedi 1–10000 olmalıdır.");
        if(string.IsNullOrWhiteSpace(job.Printer.PrinterName))throw new InvalidOperationException("Lütfen yazıcı seçin.");
        // Preflight all distinct products before the first page is sent.
        await Task.Run(()=>{foreach(var item in job.Items){using var preview=new BarcodePrinter.Printing.Rendering.LabelPreviewRenderer().Render(job.Template,item.Product,job.Printer.Dpi,job.Time);}});
        lock(sync){jobs.Add(job);try{Save();}catch{jobs.Remove(job);throw;}}Changed?.Invoke();
        await gate.WaitAsync();
        try
        {
            lock(sync){job.Status=PrintJobStatus.Printing;Save();}Changed?.Invoke();
            await Task.Run(()=>
            {
                var info=WindowsPrinterInterop.Info(job.Printer.PrinterName);
                if(job.Printer.Mode==PrintMode.RawTspl)PrinterRouting.EnsurePhysicalPort(job.Printer.PrinterName,info.PortName);
                if((info.Status&0x80)!=0)throw new InvalidOperationException("Yazıcı çevrimdışı. Bağlantıyı kontrol edin.");
                var products=job.Items.SelectMany(i=>Enumerable.Repeat(i.Product,i.Quantity)).ToList();
                if(job.Printer.Mode==PrintMode.WindowsDriver)new WindowsPrintService().Print(job.Template,products,job.Printer,job.Time);
                else
                {
                    int perPage=job.Template.Rows*job.Template.Columns;
                    using var data=new MemoryStream();
                    for(int i=0;i<products.Count;i+=perPage){var bytes=new TsplLabelRenderer().Render(job.Template,products.Skip(i).Take(perPage).ToList(),job.Printer,job.Time);if(data.Length+bytes.Length>100000000)throw new InvalidOperationException("Baskı verisi 100 MB sınırını aşıyor; işi bölün.");data.Write(bytes);}
                    new RawPrinterService().Send(job.Printer.PrinterName,data.ToArray(),job.Template.Name);
                }
            });
            lock(sync){job.Status=PrintJobStatus.Completed;job.Detail="Windows spooler'a teslim edildi. Fiziksel çıkış doğrulanmadı.";Save();}
        }
        catch(Exception ex){JsonStore.Log(ex);lock(sync){job.Status=PrintJobStatus.Failed;job.Detail=ex.Message+" Kısmi baskı oluşmuş olabilir; tekrar basmadan kontrol edin.";Save();}}
        finally{gate.Release();Changed?.Invoke();}
    }
}


