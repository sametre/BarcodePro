using System.ComponentModel;
using System.Text.Json.Serialization;
namespace BarcodePrinter.Models.LabelDesigner;

public enum BarcodeKind { EAN13, EAN8, Code128, Code39, UPCA, UPCE, ITF, Codabar, QRCode }
public enum MediaKind { Continuous, GapLabel, BlackMark }
public enum TextAlignment { Left, Center, Right }
public enum ImageFit { KeepAspectRatio, Stretch, Fit }
public enum PriceFormat { SuffixSymbol, PrefixSymbol, DotTL, CommaTL }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextElement), "text")]
[JsonDerivedType(typeof(BarcodeElement), "barcode")]
[JsonDerivedType(typeof(QrCodeElement), "qr")]
[JsonDerivedType(typeof(ImageElement), "image")]
[JsonDerivedType(typeof(RectangleElement), "rectangle")]
[JsonDerivedType(typeof(LineElement), "line")]
[JsonDerivedType(typeof(CompositeTextElement), "price")]
public abstract class LabelElement
{
    [Browsable(false)] public Guid Id { get; set; } = Guid.NewGuid();
    [Category("Genel")] public string Name { get; set; } = "Nesne";
    [Category("Konum (mm)")] public double Xmm { get; set; } = 2;
    [Category("Konum (mm)")] public double Ymm { get; set; } = 2;
    [Category("Konum (mm)")] public double WidthMm { get; set; } = 30;
    [Category("Konum (mm)")] public double HeightMm { get; set; } = 6;
    [Category("Genel")] public double Rotation { get; set; }
    [Category("Genel")] public bool IsVisible { get; set; } = true;
    [Category("Genel")] public int ZIndex { get; set; }
    public override string ToString() => Name;
}
public class TextElement : LabelElement
{
    [Category("Metin")] public string Text { get; set; } = "{{ProductName}}";
    [Category("Metin")] public string FontName { get; set; } = "Segoe UI";
    [Category("Metin"), Description("Punto (1/72 inç)")] public float FontSize { get; set; } = 10;
    [Category("Metin")] public bool Bold { get; set; }
    [Category("Metin")] public bool Italic { get; set; }
    [Category("Metin")] public bool Underline { get; set; }
    [Category("Metin")] public TextAlignment Alignment { get; set; }
    [Category("Metin"), Description("Metin kutuya sığana kadar fontu küçültür.")] public bool AutoSize { get; set; } = true;
}
public class BarcodeElement : LabelElement
{
    [Category("Barkod")] public BarcodeKind BarcodeType { get; set; } = BarcodeKind.Code128;
    [Category("Barkod")] public string Value { get; set; } = "{{Barcode}}";
    [Category("Barkod")] public bool HumanReadableText { get; set; } = true;
    [Category("Barkod")] public bool TextAbove { get; set; }
    [Category("Barkod"), Description("En dar modülün minimum genişliği, mm.")] public double ModuleWidth { get; set; } = 0.25;
    [Category("Barkod"), Description("0: nesne yüksekliğine otomatik sığdır.")] public double BarHeight { get; set; }
}
public class QrCodeElement : LabelElement { [Category("QR")] public string Value { get; set; } = "{{Barcode}}"; }
public class ImageElement : LabelElement
{
    [Browsable(false)] public string ImageBase64 { get; set; } = "";
    [Category("Görsel")] public ImageFit Fit { get; set; } = ImageFit.KeepAspectRatio;
}
public class RectangleElement : LabelElement { [Category("Çizgi")] public double ThicknessMm { get; set; } = 0.25; }
public class LineElement : LabelElement { [Category("Çizgi")] public double ThicknessMm { get; set; } = 0.25; }
public class CompositeTextElement : TextElement { [Category("Fiyat")] public float FractionFontSize { get; set; } = 12; public CompositeTextElement() { Text = "{{Price}}"; FontSize = 24; Bold = true; HeightMm = 12; } }

public sealed class LabelTemplate
{
    [Browsable(false)] public Guid Id { get; set; } = Guid.NewGuid();
    [Category("Şablon")] public string Name { get; set; } = "Yeni etiket";
    [Category("Etiket (mm)")] public double WidthMm { get; set; } = 50;
    [Category("Etiket (mm)")] public double HeightMm { get; set; } = 30;
    [Category("Baskı")] public int Dpi { get; set; } = 203;
    [Category("Medya")] public MediaKind Media { get; set; } = MediaKind.GapLabel;
    [Category("Medya")] public double Gap { get; set; } = 2;
    [Category("Medya")] public double GapOffset { get; set; }
    [Category("Medya")] public double MarkHeight { get; set; } = 2;
    [Category("Medya")] public double MarkOffset { get; set; }
    [Category("Sayfa düzeni")] public double MarginLeft { get; set; }
    [Category("Sayfa düzeni")] public double MarginTop { get; set; }
    [Category("Sayfa düzeni")] public int Rows { get; set; } = 1;
    [Category("Sayfa düzeni")] public int Columns { get; set; } = 1;
    [Category("Sayfa düzeni")] public double HorizontalGap { get; set; } = 2;
    [Category("Sayfa düzeni")] public double VerticalGap { get; set; } = 2;
    [Category("Baskı")] public PriceFormat PriceFormat { get; set; }
    [Browsable(false)] public List<LabelElement> Elements { get; set; } = [];
    [Browsable(false)] public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Browsable(false)] public DateTime UpdatedAt { get; set; } = DateTime.Now;
    [JsonIgnore, Browsable(false)] public double PageWidthMm => MarginLeft + Columns * WidthMm + (Columns - 1) * HorizontalGap;
    [JsonIgnore, Browsable(false)] public double PageHeightMm => MarginTop + Rows * HeightMm + (Rows - 1) * VerticalGap;
    public override string ToString() => $"{Name} ({WidthMm:0.#} × {HeightMm:0.#} mm)";
}
