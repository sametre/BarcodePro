using BarcodePrinter.Controls.Common;
using BarcodePrinter.Controls.Designer;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Services.Validation;
namespace BarcodePrinter.Forms.Designer;
public sealed class LabelDesignerForm : AppDialog
{
    private readonly LabelCanvas canvas=new();private readonly PropertyGrid properties=new(){Dock=DockStyle.Fill,HelpVisible=true};
    private readonly TemplateService service=new();private readonly DesignerHistory history=new();
    private readonly System.Windows.Forms.Timer autosave=new(){Interval=1600};
    private readonly Label status=new(){Dock=DockStyle.Bottom,Height=30};
    private List<LabelElement> clipboard=[];private LabelTemplate checkpoint;private bool dirty;
    public LabelTemplate Template=>canvas.Template;
    public LabelDesignerForm(LabelTemplate? template=null,Product? product=null)
    {
        Text="Etiket Tasarım Stüdyosu";Size=new Size(1366,820);MinimumSize=new Size(1050,650);KeyPreview=true;
        canvas.Template=JsonStore.Clone(template??TemplateService.Standard("Yeni etiket"));canvas.PreviewProduct=product??DynamicFields.Sample;checkpoint=JsonStore.Clone(Template);
        var toolbar=new AppToolbar();var viewbar=new AppToolbar();
        void Tool(string label,Action action)=>toolbar.Controls.Add(Form1.Button(label,()=>Try(action),false));
        Tool("Yeni",()=>{if(!dirty || ConfirmDiscard()){canvas.Template=new LabelTemplate();Reset();}});Tool("Kaydet",Save);Tool("Farklı kaydet",()=>{Template.Id=Guid.NewGuid();Template.Name+=" kopya";Save();});
        Tool("Geri al",Undo);Tool("İleri al",Redo);Tool("Öne",()=>Edit(()=>{foreach(var e in canvas.Selected.ToList()) e.ZIndex=Template.Elements.Max(x=>x.ZIndex)+1;}));Tool("Arkaya",()=>Edit(()=>{foreach(var e in canvas.Selected.ToList())e.ZIndex=Template.Elements.Min(x=>x.ZIndex)-1;}));
        Tool("Görsel değiştir",ReplaceImage); Tool("Etiket ayarları",()=>{properties.SelectedObject=Template;});
        var sizes=new AppComboBox{Width=125};foreach(var size in new[]{"30 × 20","40 × 20","40 × 30","50 × 25","50 × 30","50 × 40","60 × 40","70 × 40","80 × 50","100 × 50","100 × 100"})sizes.Items.Add(size);
        sizes.SelectedItem=$"{Template.WidthMm} × {Template.HeightMm}";
        sizes.SelectedIndexChanged+=(_,_)=>Try(()=>Edit(()=>{var parts=sizes.Text.Split('×');double w=double.Parse(parts[0]),h=double.Parse(parts[1]);if(Template.Elements.Any(e=>e.Xmm+e.WidthMm>w||e.Ymm+e.HeightMm>h))throw new InvalidOperationException("Nesneler yeni etikete sığmıyor; önce taşıyın veya boyutlandırın.");Template.WidthMm=w;Template.HeightMm=h;}));viewbar.Controls.Add(new Label{Text="Etiket boyutu",AutoSize=true});viewbar.Controls.Add(sizes);
        var grid=new AppComboBox{Width=70};grid.Items.AddRange(["1 mm","2 mm","5 mm"]);grid.SelectedIndex=0;grid.SelectedIndexChanged+=(_,_)=>{canvas.GridMm=new[]{1,2,5}[grid.SelectedIndex];canvas.Invalidate();};viewbar.Controls.Add(new Label{Text="Izgara",AutoSize=true});viewbar.Controls.Add(grid);
        var snap=new AppToggle{Text="Izgaraya yapış",Checked=true};snap.CheckedChanged+=(_,_)=>canvas.SnapToGrid=snap.Checked;viewbar.Controls.Add(snap);
        var zoom=new AppComboBox{Width=80};zoom.Items.AddRange(["50%","75%","100%","125%","150%"]);zoom.SelectedIndex=2;zoom.SelectedIndexChanged+=(_,_)=>{canvas.Zoom=new[]{.5,.75,1,1.25,1.5}[zoom.SelectedIndex];canvas.Invalidate();};viewbar.Controls.Add(new Label{Text="Yakınlaştırma",AutoSize=true});viewbar.Controls.Add(zoom);
        var toolbox=new AppToolbar();
        foreach(var name in new[]{"Metin","Ürün Adı","Fiyat","Büyük Fiyat","Eski Fiyat","Barkod","SKU","Kategori","Birim","Açıklama","Tarih","Saat","Sabit Metin","Çizgi","Dikdörtgen","Görsel","Logo","QR Kod"})
        {
            var b=Form1.Button(name,()=>Try(()=>Add(name,new PointF(2,2))),false);b.Width=108;b.Height=28;b.AutoSize=false;b.Margin=new Padding(0,0,4,3);Point origin=Point.Empty;
            b.MouseDown+=(_,e)=>origin=e.Location;b.MouseMove+=(_,e)=>{if(e.Button==MouseButtons.Left && (Math.Abs(e.X-origin.X)>5||Math.Abs(e.Y-origin.Y)>5))b.DoDragDrop(name,DragDropEffects.Copy);};toolbox.Controls.Add(b);
        }
        var right=new Panel{Dock=DockStyle.Right,Width=275};right.Controls.Add(properties);right.Controls.Add(new Label{Text="ÖZELLİKLER",Dock=DockStyle.Top,Height=34,Padding=new Padding(8,4,8,0)});
        Controls.Add(canvas);Controls.Add(right);Controls.Add(toolbox);Controls.Add(viewbar);Controls.Add(toolbar);Controls.Add(status);
        canvas.SelectionChanged+=()=>properties.SelectedObjects=canvas.Selected.Cast<object>().ToArray();canvas.BeginEdit+=()=>{history.Push(Template);};canvas.Edited+=Changed;canvas.ToolDropped+=(name,p)=>Try(()=>Add(name,p));
        properties.PropertyValueChanged+=(_,e)=>{try{ValidationService.Template(Template);history.Push(checkpoint);Changed();}catch(Exception ex){canvas.Template=JsonStore.Clone(checkpoint);properties.SelectedObject=Template;MessageBox.Show(this,ex.Message,"Geçersiz özellik");canvas.Invalidate();}};
        autosave.Tick+=(_,_)=>{autosave.Stop();Try(()=>{service.SaveDraft(Template);status.Text="Taslak otomatik kaydedildi · "+DateTime.Now.ToString("HH:mm:ss");});};
        FormClosing+=(_,e)=>{if(dirty&&!ConfirmDiscard())e.Cancel=true;};
        Shown+=(_,_)=>Try(()=>{var draft=service.Draft(Template.Id);if(draft!=null && draft.UpdatedAt>Template.UpdatedAt && MessageBox.Show(this,"Daha yeni otomatik taslak bulundu. Kurtarılsın mı?","Taslak",MessageBoxButtons.YesNo)==DialogResult.Yes){canvas.Template=draft;Reset();dirty=true;}});
        var menu=new AppContextMenu();
        void Menu(string title,Action action)=>menu.Items.Add(title,null,(_,_)=>Try(action));
        Menu("Kopyala",()=>clipboard=canvas.Selected.Select(e=>JsonStore.Clone<LabelElement>(e)).ToList());Menu("Yapıştır",Paste);Menu("Çoğalt",()=>{clipboard=canvas.Selected.Select(e=>JsonStore.Clone<LabelElement>(e)).ToList();Paste();});Menu("Sil",()=>Edit(()=>Template.Elements.RemoveAll(e=>canvas.Selection.Contains(e.Id))));menu.Items.Add(new ToolStripSeparator());Menu("Öne getir",()=>Edit(()=>{foreach(var e in canvas.Selected.ToList())e.ZIndex=Template.Elements.Max(x=>x.ZIndex)+1;}));Menu("Arkaya gönder",()=>Edit(()=>{foreach(var e in canvas.Selected.ToList())e.ZIndex=Template.Elements.Min(x=>x.ZIndex)-1;}));menu.Items.Add(new ToolStripSeparator());Menu("Geri al",Undo);Menu("İleri al",Redo);Menu("Tümünü seç",canvas.SelectAll);Menu("Etiket ayarları",()=>properties.SelectedObject=Template);
        canvas.ContextMenuStrip=menu;canvas.Disposed+=(_,_)=>menu.Dispose();
        properties.SelectedObject=Template;status.Text="Nesne eklemek için tıklayın veya sürükleyin. Sağ alt köşeden boyutlandırın. • Önizleme: "+canvas.PreviewProduct.Name;
    }
    private void Try(Action action){try{action();}catch(Exception ex)when(ex is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException){JsonStore.Log(ex);MessageBox.Show(this,ex.Message,"İşlem tamamlanamadı",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    private void Reset(){history.Clear();autosave.Stop();canvas.Selection.Clear();checkpoint=JsonStore.Clone(Template);properties.SelectedObject=Template;canvas.Invalidate();dirty=false;}
    private void Changed(){Template.UpdatedAt=DateTime.Now;checkpoint=JsonStore.Clone(Template);canvas.Invalidate();properties.Refresh();dirty=true;autosave.Stop();autosave.Start();status.Text=$"{Template.Name} · {Template.WidthMm} × {Template.HeightMm} mm · {Template.Elements.Count} nesne · Değişiklikler kaydedilmedi";}
    private void Edit(Action action){var before=JsonStore.Clone(Template);try{action();ValidationService.Template(Template);history.Push(before);Changed();}catch{canvas.Template=before;canvas.Invalidate();throw;}}
    private void Save(){service.Save(Template);dirty=false;autosave.Stop();status.Text="Şablon kaydedildi · "+Template.Name;}
    private bool ConfirmDiscard(){var answer=MessageBox.Show(this,"Değişiklikler şablona kaydedilsin mi?","Etiket tasarımı",MessageBoxButtons.YesNoCancel);if(answer==DialogResult.Cancel)return false;if(answer==DialogResult.Yes){try{Save();}catch(Exception ex){MessageBox.Show(this,ex.Message);return false;}}return true;}
    private void Undo(){canvas.Template=history.Undo(Template);canvas.Selection.Clear();properties.SelectedObject=Template;Changed();}
    private void Redo(){canvas.Template=history.Redo(Template);canvas.Selection.Clear();properties.SelectedObject=Template;Changed();}
    private void ReplaceImage()
    {
        if(canvas.Selected.FirstOrDefault() is not ImageElement image) throw new InvalidOperationException("Önce bir görsel veya logo seçin.");
        using var file=new OpenFileDialog{Filter="Görsel|*.png;*.jpg;*.jpeg;*.bmp"};if(file.ShowDialog(this)!=DialogResult.OK)return;
        if(new FileInfo(file.FileName).Length>10000000)throw new InvalidOperationException("Görsel 10 MB'dan küçük olmalı.");
        using var loaded=Image.FromFile(file.FileName);var bytes=File.ReadAllBytes(file.FileName);Edit(()=>image.ImageBase64=Convert.ToBase64String(bytes));
    }
    private void Add(string kind,PointF p)
    {
        LabelElement e=kind switch {"Barkod"=>new BarcodeElement{HeightMm=12},"QR Kod"=>new QrCodeElement{WidthMm=20,HeightMm=20},"Çizgi"=>new LineElement{HeightMm=1},"Dikdörtgen"=>new RectangleElement{HeightMm=10},"Görsel" or "Logo"=>new ImageElement{WidthMm=15,HeightMm=15},"Büyük Fiyat"=>new CompositeTextElement(),_=>new TextElement{Text=kind switch{"Ürün Adı"=>"{{ProductName}}","Fiyat"=>"{{Price}}","Eski Fiyat"=>"{{OldPrice}}","SKU"=>"{{SKU}}","Kategori"=>"{{Category}}","Birim"=>"{{Unit}}","Açıklama"=>"{{Description}}","Tarih"=>"{{Date}}","Saat"=>"{{Time}}",_=>"Metin"}}};
        if(e is ImageElement image){using var file=new OpenFileDialog{Filter="Görsel|*.png;*.jpg;*.jpeg;*.bmp"};if(file.ShowDialog(this)!=DialogResult.OK)return;var info=new FileInfo(file.FileName);if(info.Length>10000000)throw new InvalidOperationException("Görsel 10 MB'dan küçük olmalı.");using var loaded=Image.FromFile(file.FileName);image.ImageBase64=Convert.ToBase64String(File.ReadAllBytes(file.FileName));}
        e.Name=kind;e.WidthMm=Math.Min(e.WidthMm,Template.WidthMm);e.HeightMm=Math.Min(e.HeightMm,Template.HeightMm);e.Xmm=Math.Clamp(p.X,0,Template.WidthMm-e.WidthMm);e.Ymm=Math.Clamp(p.Y,0,Template.HeightMm-e.HeightMm);e.ZIndex=Template.Elements.Count==0?0:Template.Elements.Max(x=>x.ZIndex)+1;
        Edit(()=>Template.Elements.Add(e));canvas.Selection.Clear();canvas.Selection.Add(e.Id);properties.SelectedObject=e;
    }
    private void Paste(){if(clipboard.Count==0)return;Edit(()=>{canvas.Selection.Clear();foreach(var original in clipboard){var e=JsonStore.Clone<LabelElement>(original);e.Id=Guid.NewGuid();e.Xmm=Math.Min(Template.WidthMm-e.WidthMm,e.Xmm+1);e.Ymm=Math.Min(Template.HeightMm-e.HeightMm,e.Ymm+1);e.ZIndex=Template.Elements.Count;Template.Elements.Add(e);canvas.Selection.Add(e.Id);}});properties.SelectedObjects=canvas.Selected.Cast<object>().ToArray();}
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        // Keep standard editing shortcuts in text/property editors.
        if(properties.ContainsFocus)return base.ProcessCmdKey(ref msg,keyData);
        bool handled=true;Try(()=>{switch(keyData){case Keys.Control|Keys.C:clipboard=canvas.Selected.Select(e=>JsonStore.Clone<LabelElement>(e)).ToList();break;case Keys.Control|Keys.V:Paste();break;case Keys.Control|Keys.D:clipboard=canvas.Selected.Select(e=>JsonStore.Clone<LabelElement>(e)).ToList();Paste();break;case Keys.Control|Keys.Z:Undo();break;case Keys.Control|Keys.Y:Redo();break;case Keys.Control|Keys.A:canvas.SelectAll();break;case Keys.Control|Keys.S:Save();break;case Keys.Delete:Edit(()=>Template.Elements.RemoveAll(e=>canvas.Selection.Contains(e.Id)));properties.SelectedObject=Template;break;default:
            var key=keyData&Keys.KeyCode;if(key is Keys.Left or Keys.Right or Keys.Up or Keys.Down){double step=(keyData&Keys.Shift)!=0?5:.1;Edit(()=>{foreach(var e in canvas.Selected){e.Xmm=Math.Clamp(e.Xmm+(key==Keys.Left?-step:key==Keys.Right?step:0),0,Template.WidthMm-e.WidthMm);e.Ymm=Math.Clamp(e.Ymm+(key==Keys.Up?-step:key==Keys.Down?step:0),0,Template.HeightMm-e.HeightMm);}});}else handled=false;break;}});return handled||base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void Dispose(bool disposing){if(disposing)autosave.Dispose();base.Dispose(disposing);}
}




