using BarcodePrinter.Models.LabelDesigner;
namespace BarcodePrinter.Services.Templates;

public static class Ttp244CeTemplate
{
    public const string PresetName="TSC TTP-244CE · Firma etiketi 60×40";
    public static LabelTemplate Create(CompanyProfile company)
    {
        bool logo=!string.IsNullOrEmpty(company.LogoBase64);
        var template=new LabelTemplate{Name=PresetName+(string.IsNullOrWhiteSpace(company.Name)?"":" · "+company.Name),WidthMm=60,HeightMm=40,Dpi=203,Gap=2,PriceFormat=PriceFormat.CommaTL,Elements=
        [
            new TextElement{Name="Firma adı",Text=string.IsNullOrWhiteSpace(company.Name)?"FİRMA ADI":company.Name,Xmm=logo?14:2,Ymm=2,WidthMm=logo?44:56,HeightMm=7,FontSize=11,Bold=true,Alignment=TextAlignment.Center},
            new LineElement{Name="Başlık ayırıcı",Xmm=2,Ymm=10,WidthMm=56,HeightMm=.3,ThicknessMm=.2},
            new TextElement{Name="Ürün adı",Text="{{ProductName}}",Xmm=2,Ymm=11,WidthMm=56,HeightMm=6,FontSize=10,Bold=true,Alignment=TextAlignment.Center},
            new BarcodeElement{Name="Ürün barkodu",Xmm=3,Ymm=18,WidthMm=54,HeightMm=13,ModuleWidth=.25,HumanReadableText=true},
            new TextElement{Name="SKU",Text="{{SKU}}",Xmm=2,Ymm=33,WidthMm=27,HeightMm=5,FontSize=8},
            new TextElement{Name="Satış fiyatı",Text="{{Price}}",Xmm=30,Ymm=32,WidthMm=28,HeightMm=6,FontSize=12,Bold=true,Alignment=TextAlignment.Right}
        ]};
        if(logo)template.Elements.Add(new ImageElement{Name="Firma logosu",ImageBase64=company.LogoBase64,Xmm=2,Ymm=2,WidthMm=10,HeightMm=7,Fit=ImageFit.KeepAspectRatio});
        return template;
    }
}
