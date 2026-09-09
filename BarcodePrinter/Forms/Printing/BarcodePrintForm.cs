using BarcodePrinter.Controls.Common;
using BarcodePrinter.Helpers;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Models.Printing;
using BarcodePrinter.Printing.Rendering;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Services.Templates;
namespace BarcodePrinter.Forms.Printing;
public sealed class BarcodePrintForm : AppDialog
{
    private readonly List<Product> products;private readonly AppDataGrid source=new(),basket=new();
    private readonly AppComboBox printers=new(){Width=250},templates=new(){Width=250},mode=new(){Width=250},dpi=new(){Width=250},media=new(){Width=250};
    private readonly AppNumericInput copies=new(){Width=250,Minimum=1,Maximum=1000,Value=1,DecimalPlaces=0};
    private readonly AppNumericInput previewPage=new(){Width=250,Minimum=1,Maximum=10000,Value=1,DecimalPlaces=0};
    private readonly PictureBox preview=new(){Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.White};
    private readonly Label total=new(){AutoSize=true},error=new(){Dock=DockStyle.Bottom,Height=44};
    private readonly List<PrintItem> items=[];private readonly PrinterSettingsService printerService=new();
    private readonly Dictionary<string,PrinterInfo> discovered=new(StringComparer.OrdinalIgnoreCase);
    private readonly AppTextBox portInfo=new(){ReadOnly=true};
    public BarcodePrintForm(IEnumerable<Product> products,IEnumerable<Product>? selected=null,LabelTemplate? template=null)
    {
        this.products=products.ToList();Text="Barkod Yazdır • Toplu etiket baskısı";Size=new Size(1320,800);MinimumSize=new Size(1060,650);
        var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Padding=new Padding(16)};split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,34));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,38));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,28));
        var leftSection=new AppSection("01  ÜRÜN SEÇİMİ");var left=leftSection.Body;var search=new AppTextBox{Dock=DockStyle.Top,PlaceholderText="Ürün ara / barkod okut → Enter"};
        void Filter(){var list=this.products.Where(p=>(p.Name+" "+p.Barcode+" "+p.Sku).Contains(search.Text,StringComparison.CurrentCultureIgnoreCase)).Take(200).ToList();source.DataSource=list.Select(p=>new{Ürün=p.Name,Barkod=p.Barcode,Fiyat=p.Price}).ToList();for(int i=0;i<list.Count;i++)source.Rows[i].Tag=list[i];}
        void AddSelection(){foreach(DataGridViewRow row in source.SelectedRows)if(row.Tag is Product p)Add(p);}
        search.TextChanged+=(_,_)=>Filter();search.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;var p=this.products.FirstOrDefault(p=>p.Barcode.Equals(search.Text.Trim(),StringComparison.OrdinalIgnoreCase));if(p!=null){Add(p);search.SelectAll();}else error.Text="Barkod bulunamadı; aşağıdaki ürünlerden seçin.";}};
        source.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0 && source.Rows[e.RowIndex].Tag is Product p)Add(p);};
        left.Controls.Add(source);var searchBar=new Panel{Dock=DockStyle.Top,Height=36};searchBar.Controls.Add(search);left.Controls.Add(searchBar);var add=Form1.Button("Seçilenleri ekle →",AddSelection);add.Dock=DockStyle.Bottom;left.Controls.Add(add);
        var middleSection=new AppSection("02  BASKI LİSTESİ");var middle=middleSection.Body;basket.ReadOnly=false;basket.AutoGenerateColumns=false;
        basket.Columns.Add(new DataGridViewTextBoxColumn{Name="Ürün",ReadOnly=true,FillWeight=130});basket.Columns.Add(new DataGridViewTextBoxColumn{Name="Barkod",ReadOnly=true});basket.Columns.Add(new DataGridViewTextBoxColumn{Name="Fiyat",ReadOnly=true,FillWeight=65});basket.Columns.Add(new DataGridViewTextBoxColumn{Name="Adet",FillWeight=50});
        basket.CellValidating+=(_,e)=>{if(e.ColumnIndex==3&&(!int.TryParse(e.FormattedValue?.ToString(),out int n)||n<1||n>10000)){e.Cancel=true;error.Text="Adet 1–10000 arasında tam sayı olmalı.";}};
        basket.CellEndEdit+=(_,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==3){items[e.RowIndex].Quantity=Convert.ToInt32(basket.Rows[e.RowIndex].Cells[3].Value);UpdateTotal();}};
        basket.SelectionChanged+=(_,_)=>RefreshPreview();middle.Controls.Add(basket);middle.Controls.Add(new Label{Text="Adet hücresini düzenleyerek miktarı belirleyin.",Dock=DockStyle.Top,Height=36,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true});var remove=Form1.Button("Seçileni kaldır",()=>{var indices=basket.SelectedRows.Cast<DataGridViewRow>().Select(r=>r.Index).OrderDescending().ToList();foreach(var i in indices)items.RemoveAt(i);RefreshBasket();},false);remove.Dock=DockStyle.Bottom;middle.Controls.Add(remove); var basketMenu=new AppContextMenu();basketMenu.Items.Add("Seçileni kaldır",null,(_,_)=>remove.PerformClick());basketMenu.Items.Add("Önizlemeyi yenile",null,(_,_)=>RefreshPreview());basket.ContextMenuStrip=basketMenu;basket.Disposed+=(_,_)=>basketMenu.Dispose();
        var rightSection=new AppSection("03  YAZICI VE ÖNİZLEME"){Margin=Padding.Empty};var right=rightSection.Body;var options=new AppFieldGrid(115);
        void Field(string label,Control control)=>options.AddField(label,control,36);
        var available=new TemplateService().List();if(template!=null){available.RemoveAll(t=>t.Id==template.Id);available.Insert(0,template);}if(available.Count==0)available.Add(TemplateService.Standard());templates.Items.AddRange(available.Cast<object>().ToArray());templates.SelectedIndex=0;
        mode.Items.AddRange(["Windows Driver","Raw TSPL (TSC)"]);mode.SelectedIndex=0;dpi.Items.AddRange([203,300,600]);dpi.SelectedIndex=0;media.Items.AddRange(Enum.GetNames<MediaKind>());media.SelectedIndex=(int)((LabelTemplate)templates.SelectedItem!).Media;
        Field("Yazıcı",printers);Field("Bağlantı portu",portInfo);Field("Şablon",templates);Field("Baskı modu",mode);Field("DPI",dpi);Field("Medya",media);Field("Adet çarpanı",copies);Field("Önizleme sayfa",previewPage);previewPage.ValueChanged+=(_,_)=>RefreshPreview();error.Height=76;
        right.Controls.Add(preview);right.Controls.Add(options);right.Controls.Add(error);
        var bottom=new AppToolbar{Dock=DockStyle.Bottom};bottom.Controls.Add(total);bottom.Controls.Add(Form1.Button("Önizlemeyi yenile",RefreshPreview,false));var print=Form1.Button("Etiketleri yazdır",()=>{});print.Click+=async(_,_)=>{if(!ValidateChildren())return;print.Enabled=false;try{var job=CreateJob();await PrintQueueService.Instance.EnqueueAsync(job);error.Text="İş işlendi. Baskı kuyruğundan sonucu kontrol edin.";}catch(Exception ex){JsonStore.Log(ex);error.Text=ex.Message;}finally{print.Enabled=true;}};bottom.Controls.Add(print);
        split.Controls.Add(leftSection,0,0);split.Controls.Add(middleSection,1,0);split.Controls.Add(rightSection,2,0);Controls.Add(split);Controls.Add(bottom);
        templates.SelectedIndexChanged+=(_,_)=>{media.SelectedIndex=(int)((LabelTemplate)templates.SelectedItem!).Media;RefreshPreview();};dpi.SelectedIndexChanged+=(_,_)=>RefreshPreview();copies.ValueChanged+=(_,_)=>{UpdateTotal();RefreshPreview();};
        printers.SelectedIndexChanged+=(_,_)=>{try{var detail=discovered.GetValueOrDefault(printers.Text);var profile=PrinterRouting.Resolve(printerService.Get(printers.Text),detail?.Driver??"");portInfo.Text=detail?.Port??"Okunamadı";dpi.SelectedItem=profile.Dpi;mode.SelectedIndex=(int)profile.Mode;mode.Enabled=!(PrinterRouting.IsTsc(profile.PrinterName,detail?.Driver??"")||profile.Model==Ttp244CePrinter.Model);dpi.Enabled=profile.Model!=Ttp244CePrinter.Model;RefreshPreview();}catch(Exception ex){error.Text=ex.Message;}};
        Shown+=async(_,_)=>{try{var found=await printerService.DiscoverAsync();if(IsDisposed)return;foreach(var printer in found)discovered[printer.Name]=printer;printers.Items.AddRange(found.Select(p=>p.Name).ToArray());if(printers.Items.Count>0)printers.SelectedIndex=Math.Max(0,printers.Items.IndexOf(printerService.Profiles().FirstOrDefault()?.PrinterName??""));else error.Text="Windows'a kurulu yazıcı bulunamadı. Önizleme kullanılabilir.";}catch(Exception ex){error.Text=ex.Message;}};
        Filter();if(selected!=null)foreach(var p in selected)Add(p);UpdateTotal();
    }
    private void Add(Product p){var existing=items.FirstOrDefault(i=>i.Product.Id==p.Id);if(existing!=null)existing.Quantity++;else items.Add(new(){Product=p.Copy()});RefreshBasket();}
    private void RefreshBasket(){basket.Rows.Clear();foreach(var i in items)basket.Rows.Add(i.Product.Name,i.Product.Barcode,i.Product.Price.ToString("C2"),i.Quantity);UpdateTotal();RefreshPreview();}
    private void UpdateTotal()=>total.Text=$"Toplam: {items.Sum(i=>(long)i.Quantity)*(int)copies.Value:N0} etiket     ";
    private PrinterProfile Profile(){var p=printerService.Get(printers.Text);p.Dpi=Convert.ToInt32(dpi.SelectedItem);p.Mode=(PrintMode)mode.SelectedIndex;return PrinterRouting.Resolve(p,discovered.GetValueOrDefault(printers.Text)?.Driver??"");}
    private LabelTemplate ChosenTemplate(){var t=JsonStore.Clone((LabelTemplate)templates.SelectedItem!);t.Media=(MediaKind)media.SelectedIndex;return t;}
    private PrintJob CreateJob()=>new(){Printer=Profile(),Template=ChosenTemplate(),Items=items.Select(i=>new PrintItem{Product=i.Product.Copy(),Quantity=checked(i.Quantity*(int)copies.Value)}).ToList()};
    private void RefreshPreview()
    {
        if(templates.SelectedItem==null||dpi.SelectedItem==null)return;
        try{var p=basket.CurrentRow!=null&&basket.CurrentRow.Index<items.Count?items[basket.CurrentRow.Index].Product:items.FirstOrDefault()?.Product??DynamicFields.Sample;
            var profile=Profile();var template=ChosenTemplate();int perPage=template.Rows*template.Columns;
            long totalLabels=items.Sum(i=>(long)i.Quantity)*(int)copies.Value;if(totalLabels>10000)throw new InvalidOperationException("Toplam etiket adedi en fazla 10000 olabilir.");
            int pages=Math.Max(1,(int)((totalLabels+perPage-1)/perPage));int pageIndex=Math.Min((int)previewPage.Value,pages)-1;
            var pageProducts=items.Count==0?new List<Product>{p}:items.SelectMany(i=>Enumerable.Repeat(i.Product,i.Quantity*(int)copies.Value)).Skip(pageIndex*perPage).Take(perPage).ToList();
            var bitmap=new LabelPreviewRenderer().RenderSheet(template,pageProducts,profile,DateTime.Now);var old=preview.Image;preview.Image=bitmap;old?.Dispose();error.Text=$"Önizleme: {pageIndex+1}/{pages} · {pageProducts.Count} etiket";
            if(discovered.TryGetValue(printers.Text,out var device)&&profile.Mode==PrintMode.RawTspl){try{PrinterRouting.ValidatePort(printers.Text,device.Port);error.Text+=" · Doğrudan TSPL baskı";}catch(InvalidOperationException ex){error.Text=ex.Message;}}}
        catch(Exception ex){preview.Image?.Dispose();preview.Image=null;error.Text=ex.Message;}
    }
    protected override void Dispose(bool disposing){if(disposing)preview.Image?.Dispose();base.Dispose(disposing);}
}





