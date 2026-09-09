using BarcodePrinter.Controls.Common;
using BarcodePrinter.Forms.Designer;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Services.Templates;
namespace BarcodePrinter;
public partial class Form1
{
    private void Templates()
    {
        var service=new TemplateService();var grid=Grid();var toolbar=new AppToolbar();List<LabelTemplate> list=[];
        void Refresh(){list=service.List();grid.DataSource=list.Select(t=>new{Şablon=t.Name,Boyut=$"{t.WidthMm} × {t.HeightMm} mm",DPI=t.Dpi,Nesne=t.Elements.Count,Satır=t.Rows,Sütun=t.Columns,Güncelleme=t.UpdatedAt}).ToList();if(service.LoadErrors.Count>0)MessageBox.Show(this,string.Join("\n",service.LoadErrors),"Okunamayan şablonlar");}
        void One(Action<LabelTemplate> action){if(grid.CurrentRow!=null&&grid.CurrentRow.Index<list.Count)Attempt(()=>action(list[grid.CurrentRow.Index]));}
        toolbar.Controls.Add(Button("+ Yeni",()=>{using var f=new LabelDesignerForm();f.ShowDialog(this);Refresh();}));toolbar.Controls.Add(Button("Düzenle",()=>One(t=>{using var f=new LabelDesignerForm(t);f.ShowDialog(this);Refresh();}),false));
        toolbar.Controls.Add(Button("Kopyala",()=>One(t=>{var copy=JsonStore.Clone(t);copy.Id=Guid.NewGuid();copy.Name+=" kopya";service.Save(copy);Refresh();}),false));
        toolbar.Controls.Add(Button("İçe aktar",()=>Attempt(()=>{using var f=new OpenFileDialog{Filter="Etiket şablonu|*.json"};if(f.ShowDialog(this)==DialogResult.OK){service.Import(f.FileName);Refresh();}}),false));
        toolbar.Controls.Add(Button("Dışa aktar",()=>One(t=>{using var f=new SaveFileDialog{Filter="Etiket şablonu|*.json",FileName="etiket.json"};if(f.ShowDialog(this)==DialogResult.OK)service.Export(t,f.FileName);}),false));
        toolbar.Controls.Add(Button("Sil",()=>One(t=>{if(MessageBox.Show(this,t.Name+" silinsin mi?","Şablon sil",MessageBoxButtons.YesNo)==DialogResult.Yes){service.Delete(t);Refresh();}}),false));
        toolbar.Controls.Add(Button("Taslak kurtar",()=>Attempt(()=>
        {
            using var chooser=new AppDialog{Text="Otomatik kaydedilen taslaklar",Size=new Size(700,500)};
            var drafts=new ListBox{Dock=DockStyle.Fill,DataSource=service.Drafts()};var open=Button("Seçileni aç",()=>{if(drafts.SelectedItem is LabelTemplate draft){using var designer=new LabelDesignerForm(draft);designer.ShowDialog(chooser);Refresh();}},false);open.Dock=DockStyle.Bottom;chooser.Controls.Add(drafts);chooser.Controls.Add(open);chooser.ShowDialog(this);
        }),false));
        toolbar.Controls.Add(Button("Hazır şablonları ekle",()=>Attempt(()=>{service.InstallPresets();Refresh();}),false));grid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)One(t=>{using var f=new LabelDesignerForm(t);f.ShowDialog(this);Refresh();});};
        content.Controls.Add(grid);content.Controls.Add(toolbar);Refresh();
    }
    private void Queue()
    {
        var grid=Grid();var toolbar=new AppToolbar();var list=PrintQueueService.Instance.List();
        void Refresh(){list=PrintQueueService.Instance.List().OrderByDescending(j=>j.Time).ToList();grid.DataSource=list.Select(j=>new{Tarih=j.Time,Yazıcı=j.Printer.PrinterName,Şablon=j.Template.Name,Ürün=string.Join(", ",j.Items.Select(i=>i.Product.Name)),Adet=j.Items.Sum(i=>i.Quantity),Durum=j.Status,Detay=j.Detail}).ToList();}
        toolbar.Controls.Add(Button("Yenile",Refresh,false));var retry=Button("Seçilen işi tekrar yazdır",()=>{},false);retry.Click+=async(_,_)=>{if(grid.CurrentRow==null)return;var job=list[grid.CurrentRow.Index];if(job.Status is Models.Printing.PrintJobStatus.Printing or Models.Printing.PrintJobStatus.Queued)return;if(MessageBox.Show(this,"Bu işin tamamı yeniden yazdırılacak. Yazıcıdaki önceki çıktıyı kontrol ettiniz mi?","Tekrar yazdır",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;retry.Enabled=false;try{await PrintQueueService.Instance.EnqueueAsync(job);}catch(Exception ex){MessageBox.Show(this,ex.Message);}finally{if(!retry.IsDisposed){retry.Enabled=true;Refresh();}}};toolbar.Controls.Add(retry);
        void Changed(){if(!grid.IsDisposed&&grid.IsHandleCreated)grid.BeginInvoke(()=>{if(!grid.IsDisposed)Refresh();});}
        PrintQueueService.Instance.Changed+=Changed;grid.Disposed+=(_,_)=>PrintQueueService.Instance.Changed-=Changed;
        content.Controls.Add(grid);content.Controls.Add(toolbar);Refresh();
    }
    private void Reports()
    {
        var grid=Grid();grid.DataSource=inventory.Data.Products.GroupBy(p=>p.Category).Select(g=>new{Kategori=g.Key,Ürün=g.Count(),Stok=g.Sum(p=>p.Stock),AlışDeğeri=g.Sum(p=>p.Stock*p.Cost).ToString("C2"),SatışDeğeri=g.Sum(p=>p.Stock*p.Price).ToString("C2"),Kritik=g.Count(p=>p.Stock<=p.Minimum),Aktif=g.Count(p=>p.Active)}).ToList();
        var toolbar=new AppToolbar();toolbar.Controls.Add(Label("Kategori bazında envanter değeri · Stok miktarları farklı birimler içerebilir."));toolbar.Controls.Add(Button("Hareket raporu / CSV",()=>ShowPage("Hareket Geçmişi"),false));content.Controls.Add(grid);content.Controls.Add(toolbar);
    }
}

