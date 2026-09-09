using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Printing.Rendering;
namespace BarcodePrinter;

public sealed class CompanySettingsPanel : UserControl
{
    private readonly CompanyProfile company;private readonly PictureBox logo=new(){SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Fill};private readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
    public CompanySettingsPanel(string? settingsPath=null,string? templatesFolder=null)
    {
        Dock=DockStyle.Fill;Padding=new Padding(18);company=CompanyProfile.Load(settingsPath);
        var name=new AppTextBox{Text=company.Name,MaxLength=120};var status=new Label{Dock=DockStyle.Bottom,Height=42,Padding=new Padding(8)};
        var fields=new AppFieldGrid(100);fields.AddField("Firma adı",name);fields.AddField("Firma logosu",logo,140);
        var left=new Panel{Dock=DockStyle.Left,Width=370,Padding=new Padding(0,0,20,0)};
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true};
        void RefreshPreview(){company.Name=name.Text;using var stream=new MemoryStream(string.IsNullOrEmpty(company.LogoBase64)?[]:Convert.FromBase64String(company.LogoBase64));var old=logo.Image;if(stream.Length==0)logo.Image=null;else{using var loaded=Image.FromStream(stream);logo.Image=new Bitmap(loaded);}old?.Dispose();var next=new LabelPreviewRenderer().Render(Ttp244CeTemplate.Create(company),DynamicFields.Sample,203);var previous=preview.Image;preview.Image=next;previous?.Dispose();}
        void Attempt(Action action){try{action();}catch(Exception ex)when(ex is InvalidOperationException or IOException or ArgumentException or FormatException or UnauthorizedAccessException){status.Text=ex.Message;}}
        actions.Controls.Add(Form1.Button("Logo seç",()=>Attempt(()=>{using var file=new OpenFileDialog{Filter="Firma logosu|*.png;*.jpg;*.jpeg;*.bmp"};if(file.ShowDialog(this)==DialogResult.OK){company.LogoBase64=CompanyProfile.ImportLogo(file.FileName);RefreshPreview();}}),false));
        actions.Controls.Add(Form1.Button("Logoyu kaldır",()=>Attempt(()=>{company.LogoBase64="";RefreshPreview();}),false));
        actions.Controls.Add(Form1.Button("Firma bilgilerini kaydet",()=>Attempt(()=>{company.Name=name.Text;company.Save(settingsPath);status.Text="Firma bilgileri kaydedildi. Giriş ekranında da kullanılacak.";})));
        actions.Controls.Add(Form1.Button("TTP-244CE şablonu oluştur",()=>Attempt(()=>{company.Name=name.Text;company.Save(settingsPath);var template=Ttp244CeTemplate.Create(company);new TemplateService(templatesFolder).Save(template);status.Text="Yeni firma şablonu Etiket Şablonları listesine eklendi.";})));
        var note=new Label{Dock=DockStyle.Fill,Padding=new Padding(0,18,0,0),Text="TSC TTP-244CE\n203 DPI · 60 × 40 mm · 2 mm etiket aralığı\n\nLogo ve firma adı yeni şablona gömülür. Mevcut tasarımlar korunur.\n\nRulonuz farklıysa Etiket Tasarım Stüdyosu'ndan ölçüyü değiştirin."};
        left.Controls.Add(note);left.Controls.Add(actions);left.Controls.Add(fields);Controls.Add(preview);Controls.Add(left);Controls.Add(status);
        name.TextChanged+=(_,_)=>Attempt(RefreshPreview);Attempt(RefreshPreview);Disposed+=(_,_)=>{logo.Image?.Dispose();preview.Image?.Dispose();};
    }
}

