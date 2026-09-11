using System.Drawing.Drawing2D;
namespace BarcodePrinter.Themes;

public static class BrandAssets
{
    public static Bitmap CreateMark(int size)
    {
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/64f,size/64f);
        g.Clear(Color.Transparent);

        // ERP module mark: stock, sales and reporting blocks over a barcode base.
        using var gradBrush=new LinearGradientBrush(new Point(0,0),new Point(64,64),Color.FromArgb(74,81,89),Color.FromArgb(37,42,48));
        using var bgPath=new GraphicsPath();
        bgPath.AddArc(0,0,20,20,180,90);bgPath.AddArc(44,0,20,20,270,90);bgPath.AddArc(44,44,20,20,0,90);bgPath.AddArc(0,44,20,20,90,90);bgPath.CloseFigure();
        g.FillPath(gradBrush,bgPath);

        using var whiteBrush=new SolidBrush(Color.FromArgb(242,244,246));
        using var accentBrush=new SolidBrush(Color.FromArgb(177,185,193));
        g.FillRectangle(whiteBrush,10,12,13,14);g.FillRectangle(accentBrush,26,12,13,14);g.FillRectangle(whiteBrush,42,12,13,14);
        using var link=new Pen(Color.FromArgb(210,216,221),2);g.DrawLine(link,16,27,16,32);g.DrawLine(link,32,27,32,32);g.DrawLine(link,48,27,48,32);g.DrawLine(link,16,31,48,31);
        using var erpFont=new Font("Segoe UI",10,FontStyle.Bold,GraphicsUnit.Pixel);using var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString("ERP",erpFont,whiteBrush,new RectangleF(9,8,46,20),sf);
        foreach(var bar in new[]{(12,2),(17,4),(24,2),(29,5),(37,2),(42,3),(49,2)})g.FillRectangle(whiteBrush,bar.Item1,38,bar.Item2,13);

        return image;
    }
    public static byte[] CreateIcon()
    {
        int[] sizes=[16,24,32,48,64,128,256];var images=new List<byte[]>();
        foreach(int size in sizes){using var image=CreateMark(size);using var stream=new MemoryStream();image.Save(stream,System.Drawing.Imaging.ImageFormat.Png);images.Add(stream.ToArray());}
        using var result=new MemoryStream();using var writer=new BinaryWriter(result);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
        for(int i=0;i<sizes.Length;i++){writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)(sizes[i]==256?0:sizes[i]));writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(images[i].Length);writer.Write(offset);offset+=images[i].Length;}
        foreach(var image in images)writer.Write(image);return result.ToArray();
    }
}
public static class AppTypography
{
    public static Font Body()=>new("Segoe UI",9f,FontStyle.Regular);
    public static Font Small()=>new("Segoe UI",8.5f,FontStyle.Regular);
    public static Font Heading()=>new("Segoe UI",14,FontStyle.Bold);
}
