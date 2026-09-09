using System.ComponentModel;
namespace BarcodePrinter.Models.Printing;
public enum PrintMode { WindowsDriver, RawTspl }
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class PrinterCalibration
{
    public override string ToString() => $"X: {XOffsetMm:0.##} mm, Y: {YOffsetMm:0.##} mm";
    public double XOffsetMm { get; set; }
    public double YOffsetMm { get; set; }
    public double HorizontalScale { get; set; } = 1;
    public double VerticalScale { get; set; } = 1;
    public void Validate() { if (!double.IsFinite(XOffsetMm) || !double.IsFinite(YOffsetMm) || Math.Abs(XOffsetMm)>20 || Math.Abs(YOffsetMm)>20 || !double.IsFinite(HorizontalScale) || !double.IsFinite(VerticalScale) || HorizontalScale < .8 || HorizontalScale > 1.2 || VerticalScale < .8 || VerticalScale > 1.2) throw new InvalidOperationException("Kalibrasyon: ofset ±20 mm, ölçek 0,8–1,2 aralığında olmalıdır."); }
}
public sealed class PrinterProfile
{
    public string PrinterName { get; set; } = "";
    public int Dpi { get; set; } = 203;
    public string Model { get; set; } = "";
    public PrintMode Mode { get; set; }
    public PrinterCalibration Calibration { get; set; } = new();
    public override string ToString() => PrinterName;
}
public sealed record PrinterInfo(string Name,string Driver,string Port,string Status,bool IsDefault,string Paper,string Dpi,bool IsTsc);
public enum PrintJobStatus { Queued, Printing, Completed, Failed }
public sealed class PrintJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Time { get; set; } = DateTime.Now;
    public PrinterProfile Printer { get; set; } = new();
    public LabelDesigner.LabelTemplate Template { get; set; } = new();
    public List<PrintItem> Items { get; set; } = [];
    public PrintJobStatus Status { get; set; }
    public string Detail { get; set; } = "";
}
public sealed class PrintItem { public Product Product { get; set; } = new(); public int Quantity { get; set; } = 1; }

