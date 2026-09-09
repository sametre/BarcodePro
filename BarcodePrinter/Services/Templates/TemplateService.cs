using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Services.Validation;
namespace BarcodePrinter.Services.Templates;
public sealed class TemplateService
{
    private readonly string folder;
    public List<string> LoadErrors { get; } = [];
    public TemplateService(string? folder = null) { this.folder = folder ?? Path.Combine(JsonStore.Root,"templates"); Directory.CreateDirectory(this.folder); }
    public List<LabelTemplate> List()
    {
        LoadErrors.Clear(); var result = new List<LabelTemplate>();
        foreach (var file in Directory.EnumerateFiles(folder,"*.json").Where(f => !f.EndsWith(".draft.json")))
            try { var t = JsonStore.Read<LabelTemplate>(file); ValidationService.Template(t); result.Add(t); } catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or InvalidOperationException) { LoadErrors.Add(Path.GetFileName(file) + ": " + ex.Message); JsonStore.Log(ex); }
        return result.OrderBy(t => t.Name).ToList();
    }
    public List<LabelTemplate> Drafts()
    {
        var drafts=new List<LabelTemplate>();
        foreach(var file in Directory.EnumerateFiles(folder,"*.draft.json"))
        {try{drafts.Add(JsonStore.Read<LabelTemplate>(file));}catch(Exception ex)when(ex is IOException or System.Text.Json.JsonException){JsonStore.Log(ex);}}
        return drafts.OrderByDescending(t=>t.UpdatedAt).ToList();
    }
    public void Save(LabelTemplate t) { ValidationService.Template(t); t.UpdatedAt = DateTime.Now; JsonStore.Save(Path.Combine(folder,t.Id + ".json"),t); }
    public void SaveDraft(LabelTemplate t) => JsonStore.Save(Path.Combine(folder,t.Id + ".draft.json"),t);
    public LabelTemplate? Draft(Guid id) { var path = Path.Combine(folder,id + ".draft.json"); return File.Exists(path) ? JsonStore.Read<LabelTemplate>(path) : null; }
    public void Delete(LabelTemplate t) { File.Delete(Path.Combine(folder,t.Id + ".json")); File.Delete(Path.Combine(folder,t.Id + ".draft.json")); }
    public LabelTemplate Import(string path) { var t = JsonStore.Read<LabelTemplate>(path); t.Id = Guid.NewGuid(); Save(t); return t; }
    public void Export(LabelTemplate t,string path) { ValidationService.Template(t); JsonStore.Save(path,t); }
    public static LabelTemplate Standard(string name = "Standart Ürün Barkodu") => new() { Name = name, Elements = [new TextElement { Name = "Ürün adı", Xmm = 2,Ymm = 2,WidthMm = 46,HeightMm = 6,Bold = true }, new BarcodeElement { Name = "Barkod", Xmm = 2,Ymm = 10,WidthMm = 46,HeightMm = 12 },new TextElement { Name = "Fiyat",Text = "{{Price}}",Xmm = 2,Ymm = 23,WidthMm = 46,HeightMm = 5,Alignment = TextAlignment.Right,Bold = true }] };
    public void InstallPresets()
    {
        if(!List().Any(t=>t.Name==Ttp244CeTemplate.PresetName))Save(Ttp244CeTemplate.Create(new CompanyProfile()));
        foreach (var name in new[] {"Market Raf Etiketi","Standart Ürün Barkodu","Küçük Barkod","İndirim Etiketi","Depo Etiketi","Koli Etiketi","QR Etiketi"})
        {
            if (List().Any(t => t.Name == name)) continue;
            var t = Standard(name);
            if (name == "QR Etiketi") t.Elements = [new QrCodeElement { Name = "QR",Xmm = 2,Ymm = 2,WidthMm = 26,HeightMm = 26 },new TextElement { Name = "SKU",Text = "{{SKU}}",Xmm = 30,Ymm = 10,WidthMm = 18,HeightMm = 12 }];
            if (name == "İndirim Etiketi") { ((TextElement)t.Elements[2]).Text = "{{OldPrice}} → {{Price}}"; }
            if (name == "Küçük Barkod") { t.WidthMm = 40;t.HeightMm = 20;t.Elements = [new BarcodeElement { Name = "Barkod",Xmm = 1,Ymm = 2,WidthMm = 38,HeightMm = 15,ModuleWidth = .15 }]; }
            if (name == "Koli Etiketi" || name == "Depo Etiketi") { t.WidthMm = 100;t.HeightMm = 50;t.Elements.Add(new TextElement { Name = "Depo bilgisi",Text = "{{SKU}} · {{Stock}} {{Unit}}",Xmm = 2,Ymm = 35,WidthMm = 90,HeightMm = 10 }); }
            Save(t);
        }
    }
}

