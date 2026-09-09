using System.Drawing.Drawing2D;
namespace BarcodePrinter.Themes;

public static class BrandAssets
{
    public static Bitmap CreateMark(int size)
    {
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/64f,size/64f);
        using var background=new SolidBrush(Color.FromArgb(35,102,197));using var path=new GraphicsPath();
        path.AddArc(0,0,20,20,180,90);path.AddArc(44,0,20,20,270,90);path.AddArc(44,44,20,20,0,90);path.AddArc(0,44,20,20,90,90);path.CloseFigure();g.FillPath(background,path);
        using var white=new SolidBrush(Color.White);foreach(var bar in new[]{(14,3),(20,5),(28,2),(33,4),(40,2),(45,5)})g.FillRectangle(white,bar.Item1,17,bar.Item2,29);
        using var pen=new Pen(Color.FromArgb(117,222,232),3){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLines(pen,[new PointF(13,12),new PointF(8,12),new PointF(8,23)]);g.DrawLines(pen,[new PointF(51,52),new PointF(56,52),new PointF(56,41)]);
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
    public static Font Body()=>new("Segoe UI",9.5f,FontStyle.Regular);
    public static Font Small()=>new("Segoe UI",9,FontStyle.Regular);
    public static Font Heading()=>new("Segoe UI",17,FontStyle.Bold);
}
