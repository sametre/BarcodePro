using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Themes;

namespace BarcodePrinter.Forms.Network;

public sealed class ClientConnectionForm : AppWindow
{
    private readonly AppTextBox address=new(){Text="http://127.0.0.1:5088"};
    private readonly AppTextBox key=new(){UseSystemPasswordChar=true};
    private readonly AppComboBox servers=new(){Width=430,DropDownStyle=ComboBoxStyle.DropDownList};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new Size(430,0)};
    public NetworkSettings? Settings { get; private set; }
    public ClientConnectionForm()
    {
        Text="Barcode Pro Client · Server bağlantısı";ClientSize=new Size(520,410);MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        var saved=NetworkSettings.Load();address.Text=saved.ServerUrl;key.Text=saved.AccessKey;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),ColumnCount=1,RowCount=10};
        layout.Controls.Add(new Label{Text="SERVER BAĞLANTISI",AutoSize=true,Font=AppTypography.Heading()});
        layout.Controls.Add(new Label{Text="Yerel ağdaki sunucuyu bulun veya adresini yazın. Erişim anahtarı Server ekranında gösterilir.",AutoSize=true,MaximumSize=new Size(430,0),Margin=new Padding(0,8,0,10)});
        var find=new AppButton{Text="Sunucuları otomatik bul",IconKind=AppIcon.Search,Width=210};
        find.Click+=async(_,_)=>{find.Enabled=false;status.Text="Yerel ağ taranıyor…";try{var found=await ServerDiscovery.FindAsync();if(IsDisposed)return;servers.Items.Clear();servers.Items.AddRange(found.Cast<object>().ToArray());if(found.Count>0)servers.SelectedIndex=0;status.Text=found.Count>0?$"{found.Count} sunucu bulundu. Erişim anahtarını girin.":"Sunucu bulunamadı. Server adresini elle girebilirsiniz.";}catch(Exception ex)when(ex is System.Net.Sockets.SocketException or OperationCanceledException){status.Text="Ağ araması tamamlanamadı: "+ex.Message;}finally{if(!IsDisposed)find.Enabled=true;}};
        servers.SelectedIndexChanged+=(_,_)=>{if(servers.SelectedItem is DiscoveredServer server)address.Text=server.Url;};
        layout.Controls.Add(find);layout.Controls.Add(servers);
        layout.Controls.Add(new Label{Text="Server adresi",AutoSize=true});layout.Controls.Add(address);
        layout.Controls.Add(new Label{Text="Erişim anahtarı",AutoSize=true,Margin=new Padding(0,10,0,0)});layout.Controls.Add(key);layout.Controls.Add(status);
        var connect=new AppButton{Text="Bağlantıyı test et ve kaydet",IconKind=AppIcon.Settings,Width=210};
        connect.Click+=async(_,_)=>
        {
            connect.Enabled=false;find.Enabled=false;status.Text="Server bağlantısı doğrulanıyor…";
            var settings=new NetworkSettings{ServerUrl=address.Text.Trim(),AccessKey=key.Text};
            try
            {
                await Task.Run(()=>{using var client=new RemoteInventoryClient(settings);if(!client.Health())throw new InvalidOperationException("Server sağlık kontrolü başarısız.");});
                if(IsDisposed)return;settings.Save();Settings=settings;DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex)when(ex is HttpRequestException or InvalidOperationException or TaskCanceledException or IOException)
            {if(!IsDisposed){status.Text="Bağlantı kurulamadı: "+ex.Message;status.ForeColor=Color.Firebrick;}}
            finally{if(!IsDisposed){connect.Enabled=true;find.Enabled=true;}}
        };
        layout.Controls.Add(connect);Controls.Add(layout);AcceptButton=connect;ThemeManager.Apply(this);
        Shown+=(_,_)=>find.PerformClick();
    }
}
