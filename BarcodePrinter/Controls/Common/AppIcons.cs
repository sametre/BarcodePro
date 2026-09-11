using System.Drawing.Drawing2D;
namespace BarcodePrinter.Controls.Common;
public enum AppIcon { Home, Products, Add, Edit, Delete, Stock, Barcode, Print, Design, Template, Settings, Report, History, Search, Save, Copy, Undo, Redo, Image, Text, Grid, Theme, Bell, Refresh, Export, Close }
public static class AppIcons
{
    private static readonly string? WindowsIconFont=FindWindowsIconFont();
    private static string? FindWindowsIconFont()
    {
        using var fonts=new System.Drawing.Text.InstalledFontCollection();
        foreach(var name in new[]{"Segoe Fluent Icons","Segoe MDL2 Assets"})if(fonts.Families.Any(f=>f.Name.Equals(name,StringComparison.OrdinalIgnoreCase)))return name;
        return null;
    }
    // Restrained semantic colors for commands without an explicit foreground.
    private static readonly Color[] ColorPalette = [
        Color.FromArgb(230, 126, 34),   // Orange - Add, New
        Color.FromArgb(52, 152, 219),   // Blue - Products, Search
        Color.FromArgb(46, 204, 113),   // Green - Save, Stock
        Color.FromArgb(155, 89, 182),   // Purple - Settings, Design
        Color.FromArgb(231, 76, 60),    // Red - Delete, Close
        Color.FromArgb(52, 73, 94)      // Dark Blue - Home, Report
    ];
    private static Color GetColorForIcon(AppIcon icon)
    {
        // Assign colors based on icon type
        return icon switch
        {
            AppIcon.Add or AppIcon.Text => ColorPalette[0],      // Orange
            AppIcon.Products or AppIcon.Search => ColorPalette[1], // Blue
            AppIcon.Save or AppIcon.Stock => ColorPalette[2],    // Green
            AppIcon.Settings or AppIcon.Design or AppIcon.Template => ColorPalette[3], // Purple
            AppIcon.Delete or AppIcon.Close => ColorPalette[4],  // Red
            AppIcon.Home or AppIcon.Report => ColorPalette[5],   // Dark Blue
            AppIcon.Edit => ColorPalette[1],                     // Blue
            AppIcon.Print => ColorPalette[0],                    // Orange
            AppIcon.Barcode => ColorPalette[2],                  // Green
            AppIcon.History => ColorPalette[5],                  // Dark Blue
            AppIcon.Copy => ColorPalette[1],                     // Blue
            AppIcon.Undo or AppIcon.Redo => ColorPalette[3],     // Purple
            AppIcon.Image => ColorPalette[0],                    // Orange
            AppIcon.Theme => ColorPalette[3],                    // Purple
            AppIcon.Bell => ColorPalette[4],                     // Red
            AppIcon.Refresh => ColorPalette[1],                  // Blue
            AppIcon.Export => ColorPalette[2],                   // Green
            AppIcon.Grid => ColorPalette[5],                     // Dark Blue
            _ => ColorPalette[1]
        };
    }
    public static AppIcon ForText(string text)
    {
        var s=text.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
        if(s.Contains("sil")||s.Contains("kaldır"))return AppIcon.Delete;
        if(s.Contains("kaydet"))return AppIcon.Save;
        if(s.Contains("geri")||s.Contains("önceki"))return AppIcon.Undo;
        if(s.Contains("ileri")||s.Contains("sonraki"))return AppIcon.Redo;
        if(s.Contains("kopy")||s.Contains("çoğalt"))return AppIcon.Copy;
        if(s.Contains("yazdır")||s.Contains("yazıcı")||s.Contains("etiket bas"))return AppIcon.Print;
        if(s.Contains("barkod")||s.Contains("okut")||s.Contains("qr"))return AppIcon.Barcode;
        if(s.Contains("tasarım")||s.Contains("tasarla"))return AppIcon.Design;
        if(s.Contains("şablon")||s.Contains("taslak"))return AppIcon.Template;
        if(s.Contains("yeni")||s.Contains("ekle"))return AppIcon.Add;
        if(s.Contains("düzenle")||s.Contains("değiştir"))return AppIcon.Edit;
        if(s.Contains("stok")||s.Contains("giriş")||s.Contains("çıkış"))return AppIcon.Stock;
        if(s.Contains("rapor"))return AppIcon.Report;
        if(s.Contains("hareket")||s.Contains("kuyruk"))return AppIcon.History;
        if(s.Contains("ayar")||s.Contains("kalibrasyon"))return AppIcon.Settings;
        if(s.Contains("görsel")||s.Contains("logo"))return AppIcon.Image;
        if(s.Contains("tema"))return AppIcon.Theme;
        if(s.Contains("bildirim"))return AppIcon.Bell;
        if(s.Contains("yenile"))return AppIcon.Refresh;
        if(s.Contains("aktar")||s.Contains("csv"))return AppIcon.Export;
        if(s.Contains("ürün"))return AppIcon.Products;
        if(s.Contains("kolon")||s.Contains("ızgara"))return AppIcon.Grid;
        if(s.Contains("dashboard"))return AppIcon.Home;
        return AppIcon.Text;
    }
    public static Bitmap Create(AppIcon icon, Color? color = null, int size=18)
    {
        // Use palette color if no color specified, otherwise use provided color
        var finalColor = color ?? GetColorForIcon(icon);
        string? glyph=icon switch{AppIcon.Home=>"\uE80F",AppIcon.Products=>"\uE8B7",AppIcon.Add=>"\uE710",AppIcon.Edit=>"\uE70F",AppIcon.Delete=>"\uE74D",AppIcon.Print=>"\uE749",AppIcon.Settings=>"\uE713",AppIcon.Search=>"\uE721",AppIcon.Save=>"\uE74E",AppIcon.Copy=>"\uE8C8",AppIcon.Undo=>"\uE7A7",AppIcon.Redo=>"\uE7A6",AppIcon.Image=>"\uEB9F",AppIcon.History=>"\uE81C",AppIcon.Theme=>"\uE708",AppIcon.Bell=>"\uE7F4",AppIcon.Refresh=>"\uE72C",AppIcon.Close=>"\uE711",AppIcon.Grid=>"\uE80A",AppIcon.Template=>"\uE8A5",AppIcon.Export=>"\uE898",_=>null};
        if(WindowsIconFont!=null&&glyph!=null)
        {
            var bitmap=new Bitmap(size,size);using var graphics=Graphics.FromImage(bitmap);graphics.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var font=new Font(WindowsIconFont,size*.86f,FontStyle.Regular,GraphicsUnit.Pixel);using var brush=new SolidBrush(finalColor);using var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};
            graphics.DrawString(glyph,font,brush,new RectangleF(0,0,size,size),format);return bitmap;
        }
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.ScaleTransform(size/20f,size/20f);using var p=new Pen(finalColor,1.75f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        void Line(float x,float y,float xx,float yy)=>g.DrawLine(p,x,y,xx,yy);
        void Box(float x,float y,float w,float h)=>g.DrawRectangle(p,x,y,w,h);
        switch(icon)
        {
            case AppIcon.Home:Line(2,9,10,2);Line(10,2,18,9);Line(4,8,4,18);Line(16,8,16,18);Line(4,18,16,18);Box(8,12,4,6);break;
            case AppIcon.Products:Box(3,4,14,13);Line(3,8,17,8);Line(8,4,8,8);Line(12,4,12,8);Line(7,12,13,12);break;
            case AppIcon.Add:g.DrawEllipse(p,2,2,16,16);Line(10,6,10,14);Line(6,10,14,10);break;
            case AppIcon.Close:Line(4,4,16,16);Line(16,4,4,16);break;
            case AppIcon.Delete:Line(3,5,17,5);Line(7,2,13,2);Line(5,5,6,18);Line(15,5,14,18);Line(6,18,14,18);Line(8,8,8,14);Line(12,8,12,14);break;
            case AppIcon.Edit:case AppIcon.Design:g.DrawPolygon(p,[new PointF(3,13),new PointF(13,3),new PointF(17,7),new PointF(7,17),new PointF(2,18)]);Line(11,5,15,9);break;
            case AppIcon.Print:Box(2,7,16,8);Box(5,2,10,5);Box(5,12,10,6);Line(14,10,15,10);break;
            case AppIcon.Barcode:foreach(int x in new[]{3,5,8,9,12,15,17})Line(x,3,x,15);Line(3,18,17,18);break;
            case AppIcon.Stock:Box(2,10,7,8);Line(13,17,13,2);Line(10,5,13,2);Line(16,5,13,2);break;
            case AppIcon.History:g.DrawEllipse(p,2,2,16,16);Line(10,5,10,10);Line(10,10,14,12);break;
            case AppIcon.Report:Line(3,2,3,18);Line(3,18,18,18);Box(6,11,2,4);Box(11,7,2,8);Box(16,3,2,12);break;
            case AppIcon.Settings:g.DrawEllipse(p,5,5,10,10);g.DrawEllipse(p,8,8,4,4);for(int i=0;i<8;i++){double a=i*Math.PI/4;Line(10+(float)Math.Cos(a)*6,10+(float)Math.Sin(a)*6,10+(float)Math.Cos(a)*8,10+(float)Math.Sin(a)*8);}break;
            case AppIcon.Save:Box(3,2,14,16);Box(6,2,8,5);Box(6,11,8,7);break;
            case AppIcon.Copy:Box(6,6,11,12);Line(3,14,3,2);Line(3,2,13,2);break;
            case AppIcon.Undo:case AppIcon.Redo:if(icon==AppIcon.Redo){g.TranslateTransform(20,0);g.ScaleTransform(-1,1);}Line(3,7,8,2);Line(3,7,8,12);g.DrawArc(p,3,7,14,11,180,210);break;
            case AppIcon.Image:Box(2,3,16,14);g.DrawEllipse(p,5,6,3,3);Line(3,16,10,10);Line(10,10,17,16);break;
            case AppIcon.Search:g.DrawEllipse(p,2,2,11,11);Line(12,12,18,18);break;
            case AppIcon.Theme:g.DrawEllipse(p,2,2,16,16);using(var b=new SolidBrush(finalColor))g.FillPie(b,2,2,16,16,90,180);break;
            case AppIcon.Bell:g.DrawArc(p,5,3,10,13,180,180);Line(5,9,4,15);Line(15,9,16,15);Line(4,15,16,15);g.DrawArc(p,8,15,4,4,0,180);break;
            case AppIcon.Refresh:g.DrawArc(p,3,3,14,14,45,290);Line(16,3,16,8);Line(12,7,16,8);break;
            case AppIcon.Export:Box(2,8,12,10);Line(10,10,18,2);Line(12,2,18,2);Line(18,2,18,8);break;
            case AppIcon.Grid:Box(2,3,16,14);Line(2,8,18,8);Line(7,3,7,17);Line(13,3,13,17);break;
            default:Box(3,2,14,16);Line(6,6,14,6);Line(6,10,14,10);Line(6,14,11,14);break;
        }
        return image;
    }
}


