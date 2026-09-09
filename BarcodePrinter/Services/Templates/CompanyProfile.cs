using BarcodePrinter.Helpers;
namespace BarcodePrinter.Services.Templates;

public sealed class CompanyProfile
{
    public string Name { get; set; }="";
    public string LogoBase64 { get; set; }="";
    public static string DefaultPath=>Path.Combine(JsonStore.Root,"company.json");
    public static CompanyProfile Load(string? path=null)=>File.Exists(path??DefaultPath)?JsonStore.Read<CompanyProfile>(path??DefaultPath):new();
    public void Save(string? path=null)
    {
        Name=Name.Trim();if(Name.Length is 0 or >120)throw new InvalidOperationException("Firma adı 1–120 karakter olmalıdır.");
        if(LogoBase64.Length>1400000)throw new InvalidOperationException("Logo çok büyük.");
        if(LogoBase64.Length>0){using var stream=new MemoryStream(Convert.FromBase64String(LogoBase64));using var image=Image.FromStream(stream);}
        JsonStore.Save(path??DefaultPath,this);
    }
    public static string ImportLogo(string path)
    {
        if(new FileInfo(path).Length>10000000)throw new InvalidOperationException("Logo dosyası en fazla 10 MB olabilir.");
        using var original=Image.FromFile(path);
        if((long)original.Width*original.Height>40000000)throw new InvalidOperationException("Logo çözünürlüğü çok büyük.");
        double scale=Math.Min(1,512d/Math.Max(original.Width,original.Height));
        using var normalized=new Bitmap(Math.Max(1,(int)(original.Width*scale)),Math.Max(1,(int)(original.Height*scale)));
        using(var g=Graphics.FromImage(normalized)){g.Clear(Color.White);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(original,new Rectangle(Point.Empty,normalized.Size));}
        using var stream=new MemoryStream();normalized.Save(stream,System.Drawing.Imaging.ImageFormat.Png);return Convert.ToBase64String(stream.ToArray());
    }
}
