using BarcodePrinter.Controls.Common;
using BarcodePrinter.Forms.Printing;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Services.Templates;
namespace BarcodePrinter.Forms.Printers;
public sealed class PrintersForm : AppDialog
{
    private readonly AppDataGrid grid=new();private readonly PropertyGrid properties=new(){Dock=DockStyle.Right,Width=330};private readonly Label status=new(){Dock=DockStyle.Bottom,Height=36};private readonly PrinterSettingsService service=new();
    public PrintersForm()
    {
        Text="Yazıcılar ve Kalibrasyon";Size=new Size(1200,720);
        var toolbar=new AppToolbar();toolbar.Controls.Add(Form1.Button("Yazıcıları yenile",async()=>await LoadPrinters(),false));
        toolbar.Controls.Add(Form1.Button("Profili kaydet",()=>{try{if(properties.SelectedObject is PrinterProfile p){service.Save(p);status.Text="Yazıcı profili kaydedildi.";}}catch(Exception ex){status.Text=ex.Message;}}));
        toolbar.Controls.Add(Form1.Button("TTP-244CE profili",()=>{if(properties.SelectedObject is PrinterProfile p){properties.SelectedObject=Ttp244CePrinter.Create(p.PrinterName);status.Text="TTP-244CE · 203 DPI · Raw TSPL hazır. Profili kaydet düğmesiyle uygulayın.";}else status.Text="Önce kurulu yazıcıyı seçin.";},false));
        toolbar.Controls.Add(Form1.Button("Kalibrasyon test etiketi",()=>{if(properties.SelectedObject is PrinterProfile p){try{service.Save(p);var sample=DynamicFields.Sample;using var form=new BarcodePrintForm([sample],[sample],TestLabel(p));form.ShowDialog(this);}catch(Exception ex){status.Text=ex.Message;}}},false));
        grid.SelectionChanged+=(_,_)=>{if(grid.CurrentRow?.Tag is PrinterInfo p){try{properties.SelectedObject=service.Get(p.Name);status.Text=p.IsTsc?"TSC algılandı. Mode alanından RawTspl seçebilirsiniz.":"Windows Driver kullanılabilir. RawTspl yalnızca TSPL uyumlu cihazlarda seçilmelidir.";}catch(Exception ex){status.Text=ex.Message;}}};
        Controls.Add(grid);Controls.Add(properties);Controls.Add(toolbar);Controls.Add(status);Shown+=async(_,_)=>await LoadPrinters();
    }
    private async Task LoadPrinters(){try{status.Text="Yazıcılar okunuyor…";var list=await service.DiscoverAsync();if(IsDisposed)return;grid.DataSource=list.Select(p=>new{Yazıcı=p.Name,Sürücü=p.Driver,Port=p.Port,Durum=p.Status,Varsayılan=p.IsDefault,Kağıt=p.Paper,DPI=p.Dpi}).ToList();for(int i=0;i<list.Count;i++)grid.Rows[i].Tag=list[i];if(grid.Rows.Count>0){grid.CurrentCell=grid.Rows[0].Cells[0];properties.SelectedObject=service.Get(list[0].Name);}status.Text=list.Count==0?"Kurulu yazıcı bulunamadı.":"DPI ve medya boyutunu yazıcınızın teknik değerleriyle eşleştirin.";}catch(Exception ex){status.Text=ex.Message;}}
    public static LabelTemplate TestLabel(PrinterProfile p)=>new(){Name="Kalibrasyon 50×30",WidthMm=50,HeightMm=30,Dpi=p.Dpi,Elements=[new RectangleElement{Name="Sınır",Xmm=.5,Ymm=.5,WidthMm=49,HeightMm=29},new LineElement{Name="10 mm",Xmm=5,Ymm=10,WidthMm=10,HeightMm=.5},new TextElement{Name="Ölçü",Text="10 mm",Xmm=5,Ymm=5,WidthMm=12,HeightMm=4,FontSize=8},new TextElement{Name="Merkez",Text="+",Xmm=23,Ymm=13,WidthMm=4,HeightMm=4},new TextElement{Name="Bilgi",Text=$"50 × 30 mm · {p.Dpi} DPI\n{p.PrinterName}",Xmm=2,Ymm=20,WidthMm=46,HeightMm=8,FontSize=7}]};
}
