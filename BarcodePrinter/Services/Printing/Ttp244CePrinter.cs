using BarcodePrinter.Models.Printing;
using BarcodePrinter.Models.LabelDesigner;
namespace BarcodePrinter.Services.Printing;
public static class Ttp244CePrinter
{
    public const string Model="TSC TTP-244CE";
    public static PrinterProfile Create(string installedName)=>new(){PrinterName=installedName,Model=Model,Dpi=203,Mode=PrintMode.RawTspl};
    public static void Validate(LabelTemplate template,PrinterProfile profile)
    {
        if(profile.Model!=Model)return;
        if(profile.Dpi!=203)throw new InvalidOperationException("TTP-244CE profili 203 DPI kullanmalıdır.");
        if(template.PageWidthMm<20||template.PageWidthMm*profile.Calibration.HorizontalScale+Math.Max(0,profile.Calibration.XOffsetMm)>108)
            throw new InvalidOperationException("TTP-244CE: etiket sayfası en az 20 mm, kalibrasyon dahil baskı genişliği en fazla 108 mm olmalıdır.");
    }
}
