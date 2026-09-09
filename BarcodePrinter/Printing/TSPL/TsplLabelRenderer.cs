using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Rendering;
namespace BarcodePrinter.Printing.TSPL;
public sealed class TsplLabelRenderer
{
    public byte[] Render(LabelTemplate template,IReadOnlyList<Product> products,PrinterProfile profile,DateTime at)
    {
        using var bitmap=new LabelPreviewRenderer().RenderSheet(template,products,profile,at);
        var builder=new TsplCommandBuilder().SetSize(template.PageWidthMm,template.PageHeightMm);
        if(template.Media==MediaKind.BlackMark) builder.SetBlackMark(template.MarkHeight,template.MarkOffset);
        else builder.SetGap(template.Media==MediaKind.Continuous?0:template.Gap,template.Media==MediaKind.Continuous?0:template.GapOffset);
        builder.SetDirection(1).SetReference(0,0).ClearBuffer();
        int widthBytes=(bitmap.Width+7)/8;var raster=Enumerable.Repeat((byte)255,widthBytes*bitmap.Height).ToArray();
        var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try
        {
            var row=new byte[bitmap.Width*4];
            for(int y=0;y<bitmap.Height;y++) { Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length); for(int x=0;x<bitmap.Width;x++) if((row[x*4]*114+row[x*4+1]*587+row[x*4+2]*299)/1000<160) raster[y*widthBytes+x/8]&=(byte)~(0x80>>(x%8)); }
        } finally {bitmap.UnlockBits(data);}
        return builder.AddBitmap(0,0,widthBytes,bitmap.Height,raster).Print().Build();
    }
}
