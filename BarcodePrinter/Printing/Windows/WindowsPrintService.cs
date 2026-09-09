using System.Drawing.Printing;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Rendering;
namespace BarcodePrinter.Printing.Windows;
public sealed class WindowsPrintService
{
    public void Print(LabelTemplate template,IReadOnlyList<Product> products,PrinterProfile profile,DateTime at)
    {
        using var document=new PrintDocument();document.PrinterSettings.PrinterName=profile.PrinterName;
        if(!document.PrinterSettings.IsValid)throw new InvalidOperationException("Yazıcı bulunamadı veya erişim reddedildi.");
        document.DocumentName="Barcode Pro — "+template.Name;document.PrintController=new StandardPrintController();
        document.DefaultPageSettings.PaperSize=new PaperSize("Barcode Pro",(int)Math.Round(template.PageWidthMm*100/25.4),(int)Math.Round(template.PageHeightMm*100/25.4));document.DefaultPageSettings.Margins=new Margins(0,0,0,0);document.OriginAtMargins=false;
        var resolution=document.PrinterSettings.PrinterResolutions.Cast<PrinterResolution>().FirstOrDefault(r=>r.X==profile.Dpi && r.Y==profile.Dpi);if(resolution!=null)document.DefaultPageSettings.PrinterResolution=resolution;
        int index=0,perPage=template.Rows*template.Columns;
        document.PrintPage+=(_,e)=>
        {
            using var bitmap=new LabelPreviewRenderer().RenderSheet(template,products.Skip(index).Take(perPage).ToList(),profile,at);
            var g=e.Graphics??throw new InvalidOperationException("Yazıcı çizim alanı alınamadı.");g.PageUnit=GraphicsUnit.Display;
            g.TranslateTransform(-e.PageSettings.HardMarginX,-e.PageSettings.HardMarginY);
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(bitmap,new RectangleF(0,0,(float)(template.PageWidthMm*100/25.4),(float)(template.PageHeightMm*100/25.4)));
            index+=perPage;e.HasMorePages=index<products.Count;
        };
        document.Print();
    }
}
