using System.Drawing.Drawing2D;
namespace BarcodePrinter.Themes;

public static class BrandAssets
{
    public static Bitmap CreateMark(int size)
    {
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);g.ScaleTransform(size/64f,size/64f);
        using var bg=new SolidBrush(Color.FromArgb(82,88,95));using var path=new GraphicsPath();
        path.AddArc(2,2,18,18,180,90);path.AddArc(44,2,18,18,270,90);path.AddArc(44,44,18,18,0,90);path.AddArc(2,44,18,18,90,90);path.CloseFigure();g.FillPath(bg,path);
        using var ring=new Pen(Color.FromArgb(164,169,175),2);g.DrawPath(ring,path);
        using var textBrush=new SolidBrush(Color.FromArgb(240,242,244));using var font=new Font("Segoe UI",24,FontStyle.Bold,GraphicsUnit.Pixel);using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString("R3",font,textBrush,new RectangleF(4,10,56,38),format);
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
