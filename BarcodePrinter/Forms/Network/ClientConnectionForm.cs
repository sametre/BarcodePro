using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Themes;

namespace BarcodePrinter.Forms.Network;

public sealed class ClientConnectionForm : AppWindow
{
    private readonly AppTextBox address=new(){Text="http://127.0.0.1:5088"};
    private readonly AppTextBox key=new(){Text="owner",UseSystemPasswordChar=true};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new Size(430,0)};
    public NetworkSettings? Settings { get; private set; }
    public ClientConnectionForm()
    {
        Text="Barcode Pro Client · Server bağlantısı";ClientSize=new Size(520,330);MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        var saved=NetworkSettings.Load();address.Text=saved.ServerUrl;key.Text=saved.AccessKey;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(30),ColumnCount=1,RowCount=8};
        layout.Controls.Add(new Label{Text="SERVER BAĞLANTISI",AutoSize=true,Font=AppTypography.Heading()});
        layout.Controls.Add(new Label{Text="Server bilgisayarının yerel IP adresini girin. Örnek: http://192.168.1.20:5088",AutoSize=true,MaximumSize=new Size(430,0),Margin=new Padding(0,8,0,14)});
        layout.Controls.Add(new Label{Text="Server adresi",AutoSize=true});layout.Controls.Add(address);
        layout.Controls.Add(new Label{Text="Erişim anahtarı",AutoSize=true,Margin=new Padding(0,10,0,0)});layout.Controls.Add(key);layout.Controls.Add(status);
        var connect=new AppButton{Text="Bağlantıyı test et ve kaydet",IconKind=AppIcon.Settings,Width=210};
        connect.Click+=(_,_)=>Connect();layout.Controls.Add(connect);Controls.Add(layout);AcceptButton=connect;ThemeManager.Apply(this);
    }
    private void Connect()
    {
        try{var settings=new NetworkSettings{ServerUrl=address.Text.Trim(),AccessKey=key.Text};using var client=new RemoteInventoryClient(settings);if(!client.Health())throw new InvalidOperationException("Server sağlık kontrolü başarısız.");settings.Save();Settings=settings;DialogResult=DialogResult.OK;Close();}
        catch(Exception ex)when(ex is HttpRequestException or InvalidOperationException or TaskCanceledException){status.Text="Bağlantı kurulamadı: "+ex.Message;status.ForeColor=Color.Firebrick;}
    }
}
