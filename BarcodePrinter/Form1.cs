using BarcodePrinter.Controls.Common;
using BarcodePrinter.Forms.Designer;
using BarcodePrinter.Forms.Printing;
using BarcodePrinter.Forms.Printers;
using BarcodePrinter.Helpers;
using BarcodePrinter.Services.Printing;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Themes;
namespace BarcodePrinter;
public partial class Form1 : AppWindow
{
    internal static Color Ink=>ThemeManager.Current.Foreground;internal static Color Muted=>ThemeManager.Current.Muted;internal static Color Accent=>ThemeManager.Current.Accent;internal static Color Canvas=>ThemeManager.Current.Background;
    private readonly Inventory inventory;private readonly Panel content=new(){Tag="canvas",Dock=DockStyle.Fill,Padding=new Padding(24),AutoScroll=true};
    private string page="Dashboard";private readonly string dataPath;
    private readonly AppModuleMenu moduleMenu=new();private readonly AppWorkspaceTabs workspaceTabs=new();private readonly Label connection=new(){Dock=DockStyle.Bottom,Height=42,Padding=new Padding(20,10,8,8)};
    public Form1(string? inventoryPath = null)
    {
        InitializeComponent();dataPath=inventoryPath ?? Path.Combine(JsonStore.Root,"inventory.json");inventory=new Inventory(dataPath);
        Text="Barcode Pro Beta • Stok ve Etiket Yönetimi";Size=new Size(1440,900);MinimumSize=new Size(1050,700);StartPosition=FormStartPosition.CenterScreen;Font=AppTypography.Body();
        RibbonCommand Command(string title,AppIcon icon,Action action)=>new(title,icon,()=>Attempt(action));
        moduleMenu.AddModule("Giriş",AppIcon.Home,Command("Dashboard",AppIcon.Home,()=>ShowPage("Dashboard")),Command("Bildirimler",AppIcon.Bell,()=>MessageBox.Show(this,$"Kritik / tükenen: {inventory.Data.Products.Count(p=>p.Stock<=p.Minimum)} ürün\nBaşarısız baskı: {PrintQueueService.Instance.List().Count(j=>j.Status==Models.Printing.PrintJobStatus.Failed)}","Bildirimler")));
        moduleMenu.AddModule("Ürünler",AppIcon.Products,Command("Ürün listesi",AppIcon.Products,()=>ShowPage("Ürünler")),Command("Yeni ürün",AppIcon.Add,()=>EditProduct(null)),Command("Menü ve durum",AppIcon.Edit,()=>ShowPage("Menü ve durum")));
        moduleMenu.AddModule("Stok",AppIcon.Stock,Command("Stok işlemleri / barkod okut",AppIcon.Barcode,()=>ShowPage("Stok İşlemleri")),Command("Hareket geçmişi",AppIcon.History,()=>ShowPage("Hareket Geçmişi")));
        moduleMenu.AddModule("Etiket",AppIcon.Design,Command("Etiket tasarım stüdyosu",AppIcon.Design,()=>ShowPage("Etiket Tasarım Stüdyosu")),Command("Etiket şablonları",AppIcon.Template,()=>ShowPage("Etiket Şablonları")));
        moduleMenu.AddModule("Baskı",AppIcon.Print,Command("Barkod / etiket yazdır",AppIcon.Print,()=>OpenPrint()),Command("Baskı kuyruğu",AppIcon.History,()=>ShowPage("Baskı Kuyruğu")));
        moduleMenu.AddModule("Raporlar",AppIcon.Report,Command("Stok raporları",AppIcon.Report,()=>ShowPage("Raporlar")),Command("Hareket raporu / CSV",AppIcon.Export,()=>ShowPage("Hareket Geçmişi")));
        moduleMenu.AddModule("Yazıcılar",AppIcon.Print,Command("Yazıcılar ve kalibrasyon",AppIcon.Settings,()=>ShowPage("Yazıcılar")));
        moduleMenu.AddModule("Ayarlar",AppIcon.Settings,Command("Ayarlar ve yedekleme",AppIcon.Settings,()=>ShowPage("Ayarlar")),Command("Temayı değiştir",AppIcon.Theme,()=>{ThemeManager.Toggle();SelectNavigation();}));
        moduleMenu.AddAction("Kapat",AppIcon.Close,Close);
        moduleMenu.AddSearch(value=>{ShowPage("Ürünler");var field=FindSearch(content);if(field!=null)field.Text=value;});
        workspaceTabs.PageSelected+=title=>Attempt(()=>ShowPage(title));        content.Padding=new Padding(18,12,18,12);connection.Height=28;connection.Padding=new Padding(18,5,8,4);connection.Font=new Font("Segoe UI",9);
        var workspaceMenu=new AppContextMenu();workspaceMenu.Items.Add("Ekranı yenile",null,(_,_)=>ShowPage(page));workspaceMenu.Items.Add("Yeni ürün",null,(_,_)=>EditProduct(null));workspaceMenu.Items.Add("Barkod yazdır",null,(_,_)=>OpenPrint());workspaceMenu.Items.Add(new ToolStripSeparator());workspaceMenu.Items.Add("Temayı değiştir",null,(_,_)=>{ThemeManager.Toggle();SelectNavigation();});content.ContextMenuStrip=workspaceMenu;content.Disposed+=(_,_)=>workspaceMenu.Dispose();
        Controls.Add(content);Controls.Add(workspaceTabs);Controls.Add(moduleMenu);Controls.Add(connection);        Shown += (_,_) => { var area = Screen.FromControl(this).WorkingArea; Size = new Size(Math.Min(Width,area.Width),Math.Min(Height,area.Height)); }; ShowPage(page);ThemeManager.Apply(this);SelectNavigation();UpdateConnection();
    }
    private static TextBox? FindSearch(Control root){foreach(Control c in root.Controls){if(c is TextBox t)return t;var found=FindSearch(c);if(found!=null)return found;}return null;}
    private void UpdateConnection(){var printer=new PrinterSettingsService().Profiles().FirstOrDefault()?.PrinterName??"Seçilmedi";connection.Text=$"Barcode Pro 2.1 Beta 4     •     Yazıcı: {printer}     •     Veri: Yerel JSON / Bağlı";}
    internal static Button Button(string text,Action action,bool primary=true){var b=new AppButton{Text=text,IconKind=AppIcons.ForText(text),Tag=primary?"primary":"secondary",BackColor=primary?Accent:ThemeManager.Current.Surface,ForeColor=primary?Color.White:Ink};b.Click+=(_,_)=>action();return b;}
    private void Attempt(Action action){try{action();}catch(Exception ex)when(ex is InvalidOperationException or IOException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException or System.ComponentModel.Win32Exception){JsonStore.Log(ex);MessageBox.Show(this,ex.Message,"İşlem tamamlanamadı",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    private void SelectNavigation(){moduleMenu.RefreshTheme();workspaceTabs.RefreshTheme();}
    private void ShowPage(string title)
    {
        if(title=="Barkod Yazdır"){OpenPrint();return;}if(title=="Etiket Tasarım Stüdyosu"){using var f=new LabelDesignerForm();f.ShowDialog(this);return;}if(title=="Yazıcılar"){using var f=new PrintersForm();f.ShowDialog(this);UpdateConnection();return;}
        page=title;workspaceTabs.ActivatePage(title);while(content.Controls.Count>0)content.Controls[0].Dispose();
        switch(title){case "Dashboard":case "Genel bakış":Dashboard();break;case "Ürünler":Products(false);break;case "Menü ve durum":Products(true);break;case "Stok İşlemleri":case "Barkod işlemleri":Barcode();break;case "Hareket Geçmişi":case "Stok hareketleri":Movements();break;case "Etiket Şablonları":Templates();break;case "Raporlar":Reports();break;case "Baskı Kuyruğu":Queue();break;default:Settings();break;}
        ThemeManager.Apply(content);SelectNavigation();
    }
    private void OpenPrint(IEnumerable<Product>? selected=null){using var form=new BarcodePrintForm(inventory.Data.Products,selected);form.ShowDialog(this);}
    private static Label Label(string text,int size=10,bool bold=false)=>new(){Text=text,AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(0,0,0,12)};
    private FlowLayoutPanel Vertical(){var p=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};content.Controls.Add(p);return p;}
    private static DataGridView Grid()=>new AppDataGrid();
}









