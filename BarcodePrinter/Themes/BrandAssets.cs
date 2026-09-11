using System.Drawing.Drawing2D;
namespace BarcodePrinter.Themes;

public static class BrandAssets
{
    public static Bitmap CreateMark(int size)
    {
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/64f,size/64f);
        g.Clear(Color.Transparent);

        // Connected ERP modules and a barcode form a compact, recognizable application mark.
        using var gradBrush=new LinearGradientBrush(new Point(0,0),new Point(64,64),Color.FromArgb(29,57,88),Color.FromArgb(14,31,52));
        using var bgPath=new GraphicsPath();
        bgPath.AddArc(0,0,20,20,180,90);bgPath.AddArc(44,0,20,20,270,90);bgPath.AddArc(44,44,20,20,0,90);bgPath.AddArc(0,44,20,20,90,90);bgPath.CloseFigure();
        g.FillPath(gradBrush,bgPath);

        using var whiteBrush=new SolidBrush(Color.FromArgb(244,248,252));
        using var blueBrush=new SolidBrush(Color.FromArgb(84,167,238));
        g.FillRectangle(whiteBrush,12,13,17,14);g.FillRectangle(blueBrush,35,13,17,14);
        using var link=new Pen(Color.FromArgb(172,194,216),2);g.DrawLine(link,20,27,20,32);g.DrawLine(link,43,27,43,32);g.DrawLine(link,20,31,43,31);
        foreach(var bar in new[]{(13,2),(18,4),(25,2),(30,5),(38,2),(43,3),(49,2)})g.FillRectangle(whiteBrush,bar.Item1,37,bar.Item2,14);

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
