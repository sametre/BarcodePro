using BarcodePrinter.Models.LabelDesigner;
using ZXing;
namespace BarcodePrinter.Services.Validation;
public static class ValidationService
{
    public static string EncodeCode39FullAscii(string value)
    {
        var result = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c > 127) throw new InvalidOperationException("Code 39 Full ASCII yalnızca Windows-1252'nin ASCII dışı olmayan karakterleri için desteklenir; barkod metnini sadeleştirin.");
            result.Append(c switch
            {
                '\0' => "%U", '\u0001' => "$A", '\u0002' => "$B", '\u0003' => "$C", '\u0004' => "$D", '\u0005' => "$E", '\u0006' => "$F", '\u0007' => "$G", '\u0008' => "$H", '\u0009' => "$I", '\u000A' => "$J", '\u000B' => "$K", '\u000C' => "$L", '\u000D' => "$M", '\u000E' => "$N", '\u000F' => "$O", '\u0010' => "$P", '\u0011' => "$Q", '\u0012' => "$R", '\u0013' => "$S", '\u0014' => "$T", '\u0015' => "$U", '\u0016' => "$V", '\u0017' => "$W", '\u0018' => "$X", '\u0019' => "$Y", '\u001A' => "$Z", '\u001B' => "%A", '\u001C' => "%B", '\u001D' => "%C", '\u001E' => "%D", '\u001F' => "%E",
                '!' => "/A", '"' => "/B", '#' => "/C", '$' => "/D", '%' => "/E", '&' => "/F", '\'' => "/G", '(' => "/H", ')' => "/I", '*' => "/J", '+' => "/K", ',' => "/L", '/' => "/O", ':' => "/Z", ';' => "%F", '<' => "%G", '=' => "%H", '>' => "%I", '?' => "%J", '@' => "%V", '[' => "%K", '\\' => "%L", ']' => "%M", '^' => "%N", '_' => "%O", '`' => "%W", '{' => "%P", '|' => "%Q", '}' => "%R", '~' => "%S", '\u007F' => "%T",
                >= 'a' and <= 'z' => $"+{char.ToUpperInvariant(c)}",
                _ => c.ToString()
            });
        }
        return result.ToString();
    }
    public static bool Ean(string value, int length)
    {
        if (value.Length != length || value.Any(c => c < '0' || c > '9')) return false;
        int sum = 0; for (int i = length - 2, weight = 3; i >= 0; i--, weight = 4 - weight) sum += (value[i] - '0') * weight;
        return (10 - sum % 10) % 10 == value[^1] - '0';
    }
    public static BarcodeFormat Format(BarcodeKind kind) => kind switch { BarcodeKind.EAN13 => BarcodeFormat.EAN_13, BarcodeKind.EAN8 => BarcodeFormat.EAN_8, BarcodeKind.Code128 => BarcodeFormat.CODE_128, BarcodeKind.Code39 => BarcodeFormat.CODE_39, BarcodeKind.UPCA => BarcodeFormat.UPC_A, BarcodeKind.UPCE => BarcodeFormat.UPC_E, BarcodeKind.ITF => BarcodeFormat.ITF, BarcodeKind.Codabar => BarcodeFormat.CODABAR, BarcodeKind.QRCode => BarcodeFormat.QR_CODE, _ => throw new InvalidOperationException("Desteklenmeyen barkod türü.") };
    public static void Barcode(string value, BarcodeKind kind, bool fullAscii=false)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Barkod verisi boş.");
        if ((kind == BarcodeKind.EAN13 && !Ean(value,13)) || (kind == BarcodeKind.EAN8 && !Ean(value,8)) || (kind == BarcodeKind.UPCA && !Ean(value,12))) throw new InvalidOperationException($"{kind}: uzunluk veya kontrol basamağı geçersiz.");
        try { new MultiFormatWriter().encode(kind==BarcodeKind.Code39&&fullAscii?EncodeCode39FullAscii(value):value, Format(kind), 1, 1); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { throw new InvalidOperationException($"{kind}: geçersiz barkod. {ex.Message}",ex); }
    }
    public static void Template(LabelTemplate t)
    {
        if (string.IsNullOrWhiteSpace(t.Name)) throw new InvalidOperationException("Şablon adı zorunlu.");
        static bool Between(double n, double min, double max) => double.IsFinite(n) && n >= min && n <= max;
        if (!Between(t.WidthMm,5,500) || !Between(t.HeightMm,5,500) || t.Rows < 1 || t.Rows > 50 || t.Columns < 1 || t.Columns > 50 || !new[] {203,300,600}.Contains(t.Dpi)) throw new InvalidOperationException("Etiket 5–500 mm, satır/sütun 1–50 ve DPI 203/300/600 olmalıdır.");
        if (new[] { t.Gap,t.GapOffset,t.MarkHeight,t.MarkOffset,t.MarginLeft,t.MarginTop,t.HorizontalGap,t.VerticalGap }.Any(v => !Between(v,0,127)) || t.PageWidthMm > 500 || t.PageHeightMm > 1000) throw new InvalidOperationException("Medya boşlukları veya sayfa boyutu geçersiz.");
        if (t.Elements == null || t.Elements.Count > 300 || t.Elements.Select(e => e.Id).Distinct().Count() != t.Elements.Count) throw new InvalidOperationException("Şablon nesne listesi geçersiz.");
        foreach (var e in t.Elements)
        {
            if (!Between(e.Xmm,0,t.WidthMm) || !Between(e.Ymm,0,t.HeightMm) || !Between(e.WidthMm,0.1,t.WidthMm) || !Between(e.HeightMm,0.1,t.HeightMm) || e.Xmm + e.WidthMm > t.WidthMm + .001 || e.Ymm + e.HeightMm > t.HeightMm + .001 || !Between(e.Rotation,-360,360)) throw new InvalidOperationException($"'{e.Name}' etiket sınırları dışında veya boyutu geçersiz.");
            if (e is RectangleElement shape && !Between(shape.ThicknessMm,.01,10) || e is LineElement line && !Between(line.ThicknessMm,.01,10)) throw new InvalidOperationException("Çizgi kalınlığı 0,01–10 mm olmalıdır.");
            if (e is CompositeTextElement price && !Between(price.FractionFontSize,1,300)) throw new InvalidOperationException("Kuruş font boyutu 1–300 olmalıdır.");
            if (e is TextElement text && (!Between(text.FontSize,1,300) || string.IsNullOrWhiteSpace(text.FontName))) throw new InvalidOperationException("Font boyutu 1–300 punto olmalıdır.");
            if (e is BarcodeElement b && (!Between(b.ModuleWidth,.05,5) || !Between(b.BarHeight,0,e.HeightMm))) throw new InvalidOperationException("Barkod modül genişliği veya çubuk yüksekliği geçersiz.");
            if (e is ImageElement img && (img.ImageBase64.Length == 0 || img.ImageBase64.Length > 14000000)) throw new InvalidOperationException("Görsel eksik veya 10 MB sınırını aşıyor.");
        }
    }
}

