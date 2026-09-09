using System.Security.Cryptography;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Themes;
using MySqlConnector;

namespace BarcodePrinter;

public sealed class MySqlConnectionPanel : UserControl
{
    private readonly Inventory inventory;private readonly string settingsPath;
    private readonly AppTextBox host=new(),database=new(),username=new(),password=new(){UseSystemPasswordChar=true},table=new();
    private readonly NumericUpDown port=new(){Minimum=1,Maximum=65535,Value=3306,BorderStyle=BorderStyle.FixedSingle};
    private readonly AppComboBox tls=new();
    private readonly DataGridView mapping=new AppDataGrid();private readonly AppDataGrid preview=new();
    private readonly Label status=new(){Dock=DockStyle.Bottom,Height=44,Padding=new Padding(10),Text="Bağlantıyı ayarlayın, ardından ürünleri önizleyin."};
    private readonly CheckBox updateStock=new(){Text="Mevcut ürünlerin stoklarını da sunucudaki miktarla güncelle",AutoSize=true,Margin=new Padding(0,7,8,0)};
    private readonly AppButton save=new(){Text="Kaydet",IconKind=AppIcon.Save},test=new(){Text="Bağlantıyı test et",IconKind=AppIcon.Settings},fetch=new(){Text="Ürünleri getir",IconKind=AppIcon.Products},import=new(){Text="Envantere aktar",IconKind=AppIcon.Add,Enabled=false},cancel=new(){Text="İptal",IconKind=AppIcon.Close,Enabled=false};
    private readonly Panel editor=new(){Dock=DockStyle.Left,Width=340,Padding=new Padding(0,0,14,0)};
    private CancellationTokenSource? operation;private List<Product>? rows;private string[] fields=[];
    public MySqlConnectionPanel(Inventory inventory,string settingsPath)
    {
        this.inventory=inventory;this.settingsPath=settingsPath;Dock=DockStyle.Fill;Padding=new Padding(12);Font=AppTypography.Body();
        tls.Items.AddRange(["TLS · Sertifika doğrulamalı","TLS · Şifreli bağlantı","TLS kapalı · Yerel ağ"]);tls.SelectedIndex=0;
        var form=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=2,AutoSize=true};form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,106));form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        void Row(string name,Control control){int r=form.RowCount++;form.RowStyles.Add(new RowStyle(SizeType.Absolute,36));form.Controls.Add(new Label{Text=name,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,r);control.Dock=DockStyle.Fill;control.Margin=new Padding(0,6,0,6);form.Controls.Add(control,1,r);}
        Row("Sunucu",host);Row("Port",port);Row("Veritabanı",database);Row("Kullanıcı",username);Row("Şifre",password);Row("Güvenlik",tls);Row("Ürün tablosu",table);
        var hint=new Label{Dock=DockStyle.Fill,Text="MySQL’den tek yönlü aktarım\n\nKolonları sağdaki sekmeden eşleştirin. Tablo veya VIEW kullanılabilir.\n\nŞifre Windows hesabınıza bağlı şifrelenir. Kaynakta yalnızca okuma yapılır.",Padding=new Padding(0,16,0,0)};
        editor.Controls.Add(hint);editor.Controls.Add(form);
        mapping.ReadOnly=false;mapping.MultiSelect=false;mapping.SelectionMode=DataGridViewSelectionMode.CellSelect;mapping.AutoGenerateColumns=false;
        mapping.Columns.Add(new DataGridViewTextBoxColumn{Name="Field",HeaderText="Ürün alanı",ReadOnly=true,FillWeight=42});
        mapping.Columns.Add(new DataGridViewTextBoxColumn{Name="Column",HeaderText="MySQL kolon adı",FillWeight=58});
        var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new Point(14,7)};var mapTab=new TabPage("Alan eşleştirme"){Padding=new Padding(8)};mapTab.Controls.Add(mapping);var previewTab=new TabPage("Ürün önizleme"){Padding=new Padding(8)};previewTab.Controls.Add(preview);tabs.TabPages.AddRange([mapTab,previewTab]);
        var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=40,WrapContents=false};toolbar.Controls.AddRange([save,test,fetch,import,cancel]);
        var stockPanel=new Panel{Dock=DockStyle.Bottom,Height=32};stockPanel.Controls.Add(updateStock);
        Controls.Add(tabs);Controls.Add(editor);Controls.Add(stockPanel);Controls.Add(status);Controls.Add(toolbar);
        var labels=new Dictionary<string,string>{{"Name","Ürün adı *"},{"Barcode","Barkod *"},{"Sku","SKU *"},{"Price","Satış fiyatı"},{"Stock","Stok miktarı"},{"Cost","Alış fiyatı"},{"Category","Kategori"},{"Minimum","Minimum stok"},{"Maximum","Maksimum stok"},{"Unit","Birim"},{"Description","Açıklama"},{"Active","Aktif (0/1)"},{"OnMenu","Menüde (0/1)"},{"OldPrice","Eski fiyat"}};
        var settings=new MySqlSourceSettings();try{settings=MySqlSourceSettings.Load(settingsPath);password.Text=settings.GetPassword();}catch(Exception ex)when(ex is IOException or System.Text.Json.JsonException or CryptographicException or FormatException){status.Text="Kayıtlı bağlantı okunamadı. Bilgileri yeniden girip kaydedin.";}
        host.Text=settings.Host;port.Value=Math.Clamp(settings.Port,1,65535);database.Text=settings.Database;username.Text=settings.Username;table.Text=settings.Table;tls.SelectedIndex=settings.SslMode==MySqlSslMode.Disabled?2:settings.SslMode==MySqlSslMode.Required?1:0;
        foreach(var entry in new MySqlSourceSettings().Columns){int i=mapping.Rows.Add(labels[entry.Key],settings.Columns.GetValueOrDefault(entry.Key,entry.Value));mapping.Rows[i].Tag=entry.Key;}
        void InvalidatePreview(){rows=null;fields=[];import.Enabled=false;preview.DataSource=null;status.Text="Ayarlar değişti. Ürünleri yeniden getirin.";}
        foreach(var text in new[]{host,database,username,password,table})text.TextChanged+=(_,_)=>InvalidatePreview();port.ValueChanged+=(_,_)=>InvalidatePreview();tls.SelectedIndexChanged+=(_,_)=>InvalidatePreview();mapping.CellValueChanged+=(_,_)=>InvalidatePreview();
        save.Click+=(_,_)=>Run(async token=>{ReadSettings().Save(settingsPath);status.Text="Bağlantı ayarları kaydedildi.";await Task.CompletedTask;});
        test.Click+=(_,_)=>Run(async token=>{await new MySqlProductSource(ReadSettings()).TestAsync(token);status.Text="Bağlantı başarılı. Ürün tablosu ve seçilen kolonlar okunabiliyor.";});
        fetch.Click+=(_,_)=>Run(async token=>{rows=null;preview.DataSource=null;var current=ReadSettings();var result=await new MySqlProductSource(current).ReadAsync(token);token.ThrowIfCancellationRequested();rows=result;fields=MySqlProductSource.ValidateMapping(current).Keys.ToArray();preview.DataSource=result.Select(p=>new{Ürün=p.Name,Barkod=p.Barcode,SKU=p.Sku,Fiyat=p.Price,Stok=p.Stock}).ToList();tabs.SelectedTab=previewTab;status.Text=$"{result.Count:N0} ürün okundu · {current.Database}.{current.Table} · Henüz envantere aktarılmadı.";});
        import.Click+=(_,_)=>Run(async token=>{token.ThrowIfCancellationRequested();var result=inventory.ImportProducts(rows??throw new InvalidOperationException("Önce ürünleri getirin."),fields,updateStock.Checked);rows=null;status.Text=$"Aktarım tamamlandı: {result.Added} yeni, {result.Updated} güncellenen ürün. Yerel yedek korundu.";await Task.CompletedTask;});
        cancel.Click+=(_,_)=>operation?.Cancel();
        Disposed+=(_,_)=>operation?.Cancel();ThemeManager.Apply(this);
    }
    private MySqlSourceSettings ReadSettings()
    {
        mapping.EndEdit();var result=new MySqlSourceSettings{Host=host.Text.Trim(),Database=database.Text.Trim(),Username=username.Text.Trim(),Port=(uint)port.Value,Table=table.Text.Trim(),SslMode=tls.SelectedIndex==2?MySqlSslMode.Disabled:tls.SelectedIndex==1?MySqlSslMode.Required:MySqlSslMode.VerifyFull};
        result.SetPassword(password.Text);result.Columns=mapping.Rows.Cast<DataGridViewRow>().ToDictionary(r=>(string)r.Tag!,r=>Convert.ToString(r.Cells[1].Value)?.Trim()??"");MySqlProductSource.ValidateMapping(result);return result;
    }
    private async void Run(Func<CancellationToken,Task> action)
    {
        if(operation!=null)return;using var cancellation=new CancellationTokenSource(TimeSpan.FromMinutes(2));operation=cancellation;SetBusy(true);status.Text="İşlem sürüyor…";
        try{await action(cancellation.Token);}catch(OperationCanceledException){if(!IsDisposed)status.Text="İşlem iptal edildi veya zaman aşımına uğradı.";}
        catch(MySqlException ex){if(!IsDisposed)status.Text=$"MySQL bağlantısı/sorgusu başarısız (kod {ex.Number}). Sunucu, kullanıcı yetkisi, TLS ve kolonları kontrol edin.";}
        catch(Exception ex)when(ex is InvalidOperationException or IOException or UnauthorizedAccessException or CryptographicException or FormatException or ArgumentException){if(!IsDisposed)status.Text=ex is InvalidOperationException?ex.Message:"İşlem tamamlanamadı. Bağlantı ayarlarını ve dosya erişimini kontrol edin.";}
        finally{operation=null;if(!IsDisposed)SetBusy(false);}
    }
    private void SetBusy(bool busy){editor.Enabled=!busy;mapping.Enabled=!busy;save.Enabled=test.Enabled=fetch.Enabled=updateStock.Enabled=!busy;import.Enabled=!busy&&rows is {Count:>0};cancel.Enabled=busy;}
}
