using System.Globalization;
using System.Text;
namespace BarcodePrinter.Printing.TSPL;
public sealed class TsplCommandBuilder
{
    private readonly MemoryStream bytes=new();
    private static string N(double n) { if(!double.IsFinite(n)) throw new InvalidOperationException("Geçersiz TSPL ölçüsü.");return n.ToString("0.####",CultureInfo.InvariantCulture); }
    private TsplCommandBuilder Line(string value) { bytes.Write(Encoding.ASCII.GetBytes(value+"\r\n"));return this; }
    private static string Quote(string value) { if(value.Any(c=>c<' '||c>'~'||c=='"'||c=='\\')) throw new InvalidOperationException("Yerleşik TSPL metni yalnızca güvenli ASCII kabul eder. Türkçe için raster renderer kullanın.");return "\""+value+"\""; }
    public TsplCommandBuilder SetSize(double width,double height)=>Line($"SIZE {N(width)} mm,{N(height)} mm");
    public TsplCommandBuilder SetGap(double gap,double offset)=>Line($"GAP {N(gap)} mm,{N(offset)} mm");
    public TsplCommandBuilder SetBlackMark(double height,double offset)=>Line($"BLINE {N(height)} mm,{N(offset)} mm");
    public TsplCommandBuilder SetDirection(int direction) { if(direction is not 0 and not 1) throw new InvalidOperationException("Yön 0 veya 1 olmalıdır.");return Line($"DIRECTION {direction},0"); }
    public TsplCommandBuilder SetReference(int x,int y)=>Line($"REFERENCE {x},{y}");
    public TsplCommandBuilder ClearBuffer()=>Line("CLS");
    public TsplCommandBuilder AddText(int x,int y,string text,int rotation=0)=>Line($"TEXT {x},{y},\"3\",{rotation},1,1,{Quote(text)}");
    public TsplCommandBuilder AddBarcode(int x,int y,string type,int height,string value,int module=2,int rotation=0)=>Line($"BARCODE {x},{y},{Quote(type)},{height},1,{rotation},{module},{module},{Quote(value)}");
    public TsplCommandBuilder AddQrCode(int x,int y,string value,int cell=4)=>Line($"QRCODE {x},{y},L,{cell},A,0,M2,S7,{Quote(value)}");
    public TsplCommandBuilder AddBox(int x,int y,int right,int bottom,int thickness)=>Line($"BOX {x},{y},{right},{bottom},{thickness}");
    // TSPL uses BAR for horizontal/vertical line primitives, not an invented LINE command.
    public TsplCommandBuilder AddLine(int x,int y,int width,int thickness)=>Line($"BAR {x},{y},{width},{thickness}");
    public TsplCommandBuilder AddBitmap(int x,int y,int widthBytes,int height,byte[] raster)
    {
        if(widthBytes<=0||height<=0||raster.Length!=checked(widthBytes*height)) throw new InvalidOperationException("BITMAP veri uzunluğu geçersiz.");
        bytes.Write(Encoding.ASCII.GetBytes($"BITMAP {x},{y},{widthBytes},{height},0,"));bytes.Write(raster);return Line("");
    }
    public TsplCommandBuilder Print(int copies=1) { if(copies<1||copies>10000) throw new InvalidOperationException("Baskı adedi 1–10000 olmalıdır.");return Line($"PRINT 1,{copies}"); }
    public byte[] Build()=>bytes.ToArray();
}
