using System.Drawing.Drawing2D;
namespace BarcodePrinter.Controls.Common;
public enum AppIcon { Home, Products, Add, Edit, Delete, Stock, Barcode, Print, Design, Template, Settings, Report, History, Search, Save, Copy, Undo, Redo, Image, Text, Grid, Theme, Bell, Refresh, Export, Close }
public static class AppIcons
{
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
    public static Bitmap Create(AppIcon icon,Color color,int size=18)
    {
        var image=new Bitmap(size,size);using var g=Graphics.FromImage(image);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/20f,size/20f);using var p=new Pen(color,1.5f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
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
            case AppIcon.Theme:g.DrawEllipse(p,2,2,16,16);using(var b=new SolidBrush(color))g.FillPie(b,2,2,16,16,90,180);break;
            case AppIcon.Bell:g.DrawArc(p,5,3,10,13,180,180);Line(5,9,4,15);Line(15,9,16,15);Line(4,15,16,15);g.DrawArc(p,8,15,4,4,0,180);break;
            case AppIcon.Refresh:g.DrawArc(p,3,3,14,14,45,290);Line(16,3,16,8);Line(12,7,16,8);break;
            case AppIcon.Export:Box(2,8,12,10);Line(10,10,18,2);Line(12,2,18,2);Line(18,2,18,8);break;
            case AppIcon.Grid:Box(2,3,16,14);Line(2,8,18,8);Line(7,3,7,17);Line(13,3,13,17);break;
            default:Box(3,2,14,16);Line(6,6,14,6);Line(6,10,14,10);Line(6,14,11,14);break;
        }
        return image;
    }
}


