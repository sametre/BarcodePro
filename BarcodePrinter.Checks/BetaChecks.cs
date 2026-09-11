using BarcodePrinter;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Printing.Rendering;
using BarcodePrinter.Printing.TSPL;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Themes;
internal static class BetaChecks
{
    public static int Run(string directory)
    {
        int count=0;void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
        Check(LoginForm.Authenticate("owner","owner")&&!LoginForm.Authenticate("Owner","owner")&&!LoginForm.Authenticate("owner","wrong")&&!LoginForm.Authenticate("",""),"Owner login accepts only requested credentials");
        IEnumerable<Control> Walk(Control root){foreach(Control c in root.Controls){yield return c;foreach(var child in Walk(c))yield return child;}}
        using(var login=new LoginForm(new CompanyProfile()))
        {
            login.Show();Application.DoEvents();var inputs=Walk(login).OfType<TextBox>().ToList();var button=Walk(login).OfType<Button>().Single(b=>b.Text=="Giriş yap");
            inputs[0].Text="owner";inputs[1].Text="wrong";button.PerformClick();Check(login.DialogResult!=DialogResult.OK&&inputs[1].Text=="","Wrong password leaves login open and clears password");
            using(var image=new Bitmap(login.Width,login.Height)){login.DrawToBitmap(image,new Rectangle(Point.Empty,login.Size));image.Save(Path.Combine(directory,"login.png"));}
            inputs[1].Text="owner";button.PerformClick();Check(login.DialogResult==DialogResult.OK,"Login button returns authenticated result");
        }
        using var mark=BrandAssets.CreateMark(256);var file=Path.Combine(directory,"test-company-logo.png");mark.Save(file);
        var company=new CompanyProfile{Name="Örnek Firma",LogoBase64=CompanyProfile.ImportLogo(file)};var companyPath=Path.Combine(directory,"company.json");company.Save(companyPath);company=CompanyProfile.Load(companyPath);
        Check(company.Name=="Örnek Firma"&&company.LogoBase64.Length>0,"Company name and embedded logo persist");
        var template=Ttp244CeTemplate.Create(company);var tscBarcode=(BarcodeElement)template.Elements.Single(e=>e.Name=="Ürün barkodu");Check(tscBarcode.BarcodeType==BarcodeKind.Code39&&tscBarcode.ModuleWidth==.5&&tscBarcode.Code39FullAscii&&tscBarcode.CodePage==1252,"TSC preset uses Code 39 Full ASCII, 0.50 mm and Windows-1252");var profile=Ttp244CePrinter.Create("TEST TTP-244CE");Ttp244CePrinter.Validate(template,profile);
        using(var label=new LabelPreviewRenderer().RenderSheet(template,[DynamicFields.Sample],profile,DateTime.Now)){label.Save(Path.Combine(directory,"ttp-244ce-label.png"));Check(label.Width==480&&label.Height==320,"TTP-244CE 60x40 label renders at 203 DPI");}
        Check(template.Elements.OfType<ImageElement>().Single().ImageBase64==company.LogoBase64&&template.Elements.OfType<TextElement>().Any(t=>t.Text==company.Name),"Company branding embedded in template");
        var bytes=new TsplLabelRenderer().Render(template,[DynamicFields.Sample],profile,DateTime.Now);Check(System.Text.Encoding.ASCII.GetString(bytes.Take(150).ToArray()).Contains("SIZE 60 mm,40 mm"),"TTP-244CE TSPL label dimensions");
        profile.Dpi=300;try{Ttp244CePrinter.Validate(template,profile);throw new Exception("Expected DPI rejection");}catch(InvalidOperationException){Check(true,"TTP-244CE rejects wrong DPI");}
        profile.Dpi=203;template.Columns=2;try{Ttp244CePrinter.Validate(template,profile);throw new Exception("Expected width rejection");}catch(InvalidOperationException){Check(true,"TTP-244CE rejects page wider than printhead");}
        using(var window=new BarcodePrinter.Controls.Common.AppWindow{Size=new Size(1100,720),Text="Firma ve etiket"}){window.Controls.Add(new CompanySettingsPanel(companyPath,Path.Combine(directory,"beta-templates")));window.Show();Application.DoEvents();using var image=new Bitmap(window.Width,window.Height);window.DrawToBitmap(image,new Rectangle(Point.Empty,window.Size));image.Save(Path.Combine(directory,"company-settings.png"));window.Close();}
        return count;
    }
}
