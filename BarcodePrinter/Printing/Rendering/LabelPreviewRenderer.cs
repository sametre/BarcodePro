using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Services.Validation;
using ZXing;
namespace BarcodePrinter.Printing.Rendering;

public sealed class LabelPreviewRenderer
{
    public Bitmap Render(LabelTemplate template,Product product,int dpi,DateTime? at=null,bool strict=true)
    {
        if(strict) ValidationService.Template(template);
        int width=PrinterUnitConverter.MmToDots(template.WidthMm,dpi),height=PrinterUnitConverter.MmToDots(template.HeightMm,dpi);
        if(width<1 || height<1 || (long)width*height>40000000) throw new InvalidOperationException("Önizleme boyutu çok büyük veya geçersiz.");
        var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb); bitmap.SetResolution(dpi,dpi);
        try { using var g=Graphics.FromImage(bitmap);g.Clear(Color.White);Draw(g,template,product,dpi,at??DateTime.Now,strict); return bitmap; } catch { bitmap.Dispose();throw; }
    }
    public void Draw(Graphics g,LabelTemplate template,Product product,double dpi,DateTime at,bool strict=true)
    {
        g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        foreach(var e in template.Elements.Where(e=>e.IsVisible).OrderBy(e=>e.ZIndex))
        {
            var state=g.Save();
            float x=(float)PrinterUnitConverter.MmToPixels(e.Xmm,dpi), y=(float)PrinterUnitConverter.MmToPixels(e.Ymm,dpi), w=(float)PrinterUnitConverter.MmToPixels(e.WidthMm,dpi),h=(float)PrinterUnitConverter.MmToPixels(e.HeightMm,dpi);
            g.TranslateTransform(x+w/2,y+h/2);g.RotateTransform((float)e.Rotation);g.TranslateTransform(-w/2,-h/2);
            g.SetClip(new RectangleF(0,0,w,h),CombineMode.Intersect);
            try
            {
                switch(e)
                {
                    case CompositeTextElement price:
                        DrawPrice(g,price,product,template.PriceFormat,w,h,dpi);break;
                    case TextElement text:
                        DrawText(g,text,DynamicFields.Resolve(text.Text,product,template.PriceFormat,at),w,h,dpi);break;
                    case BarcodeElement barcode:
                        DrawBarcode(g,DynamicFields.Resolve(barcode.Value,product,template.PriceFormat,at),barcode.BarcodeType,w,h,dpi,barcode.HumanReadableText,barcode.TextAbove,barcode.ModuleWidth,barcode.BarHeight,barcode.Code39FullAscii);break;
                    case QrCodeElement qr:
                        DrawBarcode(g,DynamicFields.Resolve(qr.Value,product,template.PriceFormat,at),BarcodeKind.QRCode,w,h,dpi,false,false,.1,0);break;
                    case ImageElement image:
                        if(string.IsNullOrEmpty(image.ImageBase64)) throw new InvalidOperationException("Görsel seçilmedi.");
                        using(var stream=new MemoryStream(Convert.FromBase64String(image.ImageBase64))) using(var loaded=Image.FromStream(stream))
                        {
                            var rect=new RectangleF(0,0,w,h);if(image.Fit!=ImageFit.Stretch) { var ratio=Math.Min(w/loaded.Width,h/loaded.Height);rect=new((w-loaded.Width*ratio)/2,(h-loaded.Height*ratio)/2,loaded.Width*ratio,loaded.Height*ratio); } g.DrawImage(loaded,rect);
                        } break;
                    case RectangleElement box:
                        using(var pen=new Pen(Color.Black,(float)PrinterUnitConverter.MmToPixels(box.ThicknessMm,dpi))) g.DrawRectangle(pen,pen.Width/2,pen.Width/2,Math.Max(0,w-pen.Width),Math.Max(0,h-pen.Width));break;
                    case LineElement line:
                        using(var pen=new Pen(Color.Black,(float)PrinterUnitConverter.MmToPixels(line.ThicknessMm,dpi))) g.DrawLine(pen,0,h/2,w,h/2);break;
                }
            }
            catch(Exception ex) when(!strict && ex is ArgumentException or InvalidOperationException or System.FormatException)
            {
                using var font=new Font("Segoe UI",Math.Max(8,(float)dpi/15),GraphicsUnit.Pixel);g.DrawString(ex.Message,font,Brushes.Firebrick,new RectangleF(0,0,w,h));
            }
            finally { g.Restore(state); }
        }
    }
    private static void DrawText(Graphics g,TextElement e,string value,float w,float h,double dpi)
    {
        var style=(e.Bold?FontStyle.Bold:0)|(e.Italic?FontStyle.Italic:0)|(e.Underline?FontStyle.Underline:0);
        float size=(float)(e.FontSize*dpi/72);using var format=new StringFormat { Alignment=e.Alignment switch {TextAlignment.Center=>StringAlignment.Center,TextAlignment.Right=>StringAlignment.Far,_=>StringAlignment.Near},LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter };
        if(e.AutoSize) { for(int i=0;i<40 && size>3;i++) { using var f=new Font(e.FontName,size,style,GraphicsUnit.Pixel);var measured=g.MeasureString(value,f,new SizeF(w,100000),format);if(measured.Height<=h && measured.Width<=w+1) break;size*=.92f; } }
        using var font=new Font(e.FontName,Math.Max(1,size),style,GraphicsUnit.Pixel);g.DrawString(value,font,Brushes.Black,new RectangleF(0,0,w,h),format);
    }
    private static void DrawPrice(Graphics g,CompositeTextElement e,Product p,PriceFormat format,float w,float h,double dpi)
    {
        var amount=e.Text.Contains("OldPrice") ? p.OldPrice??0 : p.Price;
        var parts=amount.ToString("F2",CultureInfo.InvariantCulture).Split('.');
        using var whole=new Font(e.FontName,(float)(e.FontSize*dpi/72),e.Bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);
        using var fraction=new Font(e.FontName,(float)(e.FractionFontSize*dpi/72),GraphicsUnit.Pixel);
        string first=(format==PriceFormat.PrefixSymbol?"₺":"")+parts[0], second=(format==PriceFormat.DotTL?".":",")+parts[1]+(format==PriceFormat.PrefixSymbol?"":format==PriceFormat.SuffixSymbol?" ₺":" TL");
        var size=g.MeasureString(first,whole);g.DrawString(first,whole,Brushes.Black,0,Math.Max(0,(h-size.Height)/2));g.DrawString(second,fraction,Brushes.Black,size.Width-3,Math.Max(0,(h-size.Height)/2));
    }
    private static void DrawBarcode(Graphics g,string value,BarcodeKind kind,float w,float h,double dpi,bool readable,bool above,double module,double barHeight,bool fullAscii=false)
    {
        ValidationService.Barcode(value,kind,fullAscii);
        var encodedValue=kind==BarcodeKind.Code39&&fullAscii?ValidationService.EncodeCode39FullAscii(value):value;
        var qr=kind==BarcodeKind.QRCode;
        var hints=new Dictionary<EncodeHintType,object> { [EncodeHintType.MARGIN]=qr?4:10, [EncodeHintType.CHARACTER_SET]="UTF-8" };
        var matrix=new MultiFormatWriter().encode(encodedValue,ValidationService.Format(kind),0,0,hints);
        int scale=(int)Math.Floor(qr?Math.Min(w/matrix.Width,h/matrix.Height):w/matrix.Width);
        if(scale<1) throw new InvalidOperationException("Barkod kutusu dar; genişliği artırın veya modül genişliğini azaltın.");
        float textHeight=readable&&!qr?(float)(8*dpi/72*1.3):0;
        float bars=barHeight>0?Math.Min((float)PrinterUnitConverter.MmToPixels(barHeight,dpi),h-textHeight):h-textHeight;
        if(bars<1) throw new InvalidOperationException("Barkod yüksekliği yetersiz.");
        float offsetX=(w-matrix.Width*scale)/2,offsetY=above?textHeight:0;
        var smoothing=g.SmoothingMode;g.SmoothingMode=SmoothingMode.None;
        for(int yy=0;yy<matrix.Height;yy++) for(int xx=0;xx<matrix.Width;xx++) if(matrix[xx,yy]) g.FillRectangle(Brushes.Black,offsetX+xx*scale,offsetY+(qr?yy*scale:0),scale,qr?scale:bars);
        g.SmoothingMode=smoothing;
        if(readable&&!qr) { using var f=new Font("Arial",(float)(8*dpi/72),GraphicsUnit.Pixel);using var sf=new StringFormat {Alignment=StringAlignment.Center};g.DrawString(value,f,Brushes.Black,new RectangleF(0,above?0:bars,w,textHeight),sf); }
    }
    public Bitmap RenderSheet(LabelTemplate template,IReadOnlyList<Product> products,PrinterProfile profile,DateTime at)
    {
        ValidationService.Template(template);profile.Calibration.Validate();
        BarcodePrinter.Services.Printing.Ttp244CePrinter.Validate(template,profile);
        int width=PrinterUnitConverter.MmToDots(template.PageWidthMm,profile.Dpi),height=PrinterUnitConverter.MmToDots(template.PageHeightMm,profile.Dpi);
        if((long)width*height>40000000) throw new InvalidOperationException("Sayfa yüksek çözünürlükte çok büyük; satır/sütun sayısını azaltın.");
        var sheet=new Bitmap(width,height);sheet.SetResolution(profile.Dpi,profile.Dpi);
        try
        {
            using var g=Graphics.FromImage(sheet);g.Clear(Color.White);g.TranslateTransform((float)PrinterUnitConverter.MmToPixels(profile.Calibration.XOffsetMm,profile.Dpi),(float)PrinterUnitConverter.MmToPixels(profile.Calibration.YOffsetMm,profile.Dpi));g.ScaleTransform((float)profile.Calibration.HorizontalScale,(float)profile.Calibration.VerticalScale);
            for(int i=0;i<Math.Min(products.Count,template.Rows*template.Columns);i++)
            {
                var state=g.Save();double x=template.MarginLeft+(i%template.Columns)*(template.WidthMm+template.HorizontalGap),y=template.MarginTop+(i/template.Columns)*(template.HeightMm+template.VerticalGap);
                g.TranslateTransform((float)PrinterUnitConverter.MmToPixels(x,profile.Dpi),(float)PrinterUnitConverter.MmToPixels(y,profile.Dpi));g.SetClip(new RectangleF(0,0,(float)PrinterUnitConverter.MmToPixels(template.WidthMm,profile.Dpi),(float)PrinterUnitConverter.MmToPixels(template.HeightMm,profile.Dpi)),CombineMode.Intersect);Draw(g,template,products[i],profile.Dpi,at);g.Restore(state);
            }
            return sheet;
        } catch { sheet.Dispose();throw; }
    }
}

