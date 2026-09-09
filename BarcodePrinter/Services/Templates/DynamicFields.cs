using System.Globalization;
using BarcodePrinter.Models.LabelDesigner;
namespace BarcodePrinter.Services.Templates;
public static class DynamicFields
{
    public static string Price(decimal price, PriceFormat format) => format switch { PriceFormat.PrefixSymbol => "₺" + price.ToString("N2",CultureInfo.GetCultureInfo("tr-TR")), PriceFormat.DotTL => price.ToString("F2",CultureInfo.InvariantCulture) + " TL", PriceFormat.CommaTL => price.ToString("N2",CultureInfo.GetCultureInfo("tr-TR")) + " TL", _ => price.ToString("N2",CultureInfo.GetCultureInfo("tr-TR")) + " ₺" };
    public static string Resolve(string text, Product p, PriceFormat format, DateTime? at = null)
    {
        var now = at ?? DateTime.Now;
        var fields = new Dictionary<string,string> { ["ProductName"] = p.Name, ["Barcode"] = p.Barcode, ["SKU"] = p.Sku, ["Price"] = Price(p.Price,format), ["OldPrice"] = p.OldPrice.HasValue ? Price(p.OldPrice.Value,format) : "", ["Category"] = p.Category, ["Unit"] = p.Unit, ["Description"] = p.Description, ["Stock"] = p.Stock.ToString("0.###",CultureInfo.GetCultureInfo("tr-TR")), ["Date"] = now.ToString("dd.MM.yyyy"), ["Time"] = now.ToString("HH:mm") };
        return System.Text.RegularExpressions.Regex.Replace(text,@"\{\{(\w+)\}\}",m => fields.TryGetValue(m.Groups[1].Value,out var value) ? value : throw new InvalidOperationException("Bilinmeyen veri alanı: " + m.Value));
    }
    public static Product Sample => new() { Name = "Coca Cola 1L", Barcode = "8690000000005", Sku = "ICE-001", Category = "İçecek", Unit = "Adet", Price = 49.90m, OldPrice = 59.90m, Stock = 24, Description = "Serin servis ediniz" };
}

