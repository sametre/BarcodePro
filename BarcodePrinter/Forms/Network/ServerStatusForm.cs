using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Themes;

namespace BarcodePrinter.Forms.Network;

public sealed class ServerStatusForm : AppWindow
{
    private readonly AppTextBox state=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    private readonly System.Windows.Forms.Timer timer=new(){Interval=15000};
    private bool refreshing;
    private string publicIp="Alınıyor…";
    public ServerStatusForm()
    {
        Text="Barcode Pro Server · Servis yönetimi";ClientSize=new Size(650,510);MinimumSize=new Size(580,440);StartPosition=FormStartPosition.CenterScreen;
        var copy=new AppButton{Text="Bilgileri kopyala",IconKind=AppIcon.Export,Width=145,Height=28};copy.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(state.Text))Clipboard.SetText(state.Text);};
        var refresh=new AppButton{Text="Yenile",IconKind=AppIcon.Settings,Width=95,Height=28};refresh.Click+=async(_,_)=>await RefreshStatus();
        var start=new AppButton{Text="Servisi başlat",IconKind=AppIcon.Stock,Width=135,Height=28};start.Click+=async(_,_)=>{try{using var service=new ServiceController(ServerConfiguration.ServiceName);if(service.Status==ServiceControllerStatus.Stopped){service.Start();await Task.Run(()=>service.WaitForStatus(ServiceControllerStatus.Running,TimeSpan.FromSeconds(20)));}await RefreshStatus();}catch(Exception ex){state.Text="Servis başlatılamadı: "+ex.Message;}};
        var actions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=36,WrapContents=false};actions.Controls.AddRange([copy,refresh,start]);
        var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(18)};panel.Controls.Add(state);panel.Controls.Add(actions);Controls.Add(panel);
        Shown+=async(_,_)=>{await RefreshStatus();publicIp=await ServerConfiguration.PublicAddressAsync();if(!IsDisposed){await RefreshStatus();timer.Start();}};
        timer.Tick+=async(_,_)=>await RefreshStatus();FormClosed+=(_,_)=>timer.Dispose();ThemeManager.Apply(this);
    }
    private async Task RefreshStatus()
    {
        if(refreshing||IsDisposed)return;
        refreshing=true;
        try
        {
            var settings=ServerConfiguration.Load();
            using var client=new RemoteInventoryClient(settings.LocalClient());
            bool healthy=await Task.Run(()=>{try{return client.Health();}catch(HttpRequestException){return false;}catch(TaskCanceledException){return false;}});
            if(IsDisposed)return;
            using var service=new ServiceController(ServerConfiguration.ServiceName);
            var ips=ServerConfiguration.LocalAddresses().Select(x=>$"http://{x}:{settings.Port}");
            state.Text="BARCODE PRO SERVER · "+(healthy?"HAZIR":"BAĞLANTI BEKLENİYOR")+"\r\n"+
                $"\r\nWindows servisi: {service.Status}\r\nBilgisayar: {Environment.MachineName}\r\nPort: {settings.Port}\r\n"+
                "\r\nCLIENT BAĞLANTI ADRESLERİ\r\n"+string.Join("\r\n",ips)+$"\r\nhttp://{Environment.MachineName}:{settings.Port}"+
                "\r\n\r\nErişim anahtarı: "+settings.AccessKey+
                "\r\n\r\nDış IP: "+publicIp+"\r\nDış ağ bağlantısı için kurumsal VPN veya HTTPS ağ geçidi gerekir."+
                "\r\n\r\nSQLite: "+ServerConfiguration.InventoryPath+
                "\r\n\r\nWindows açılışında otomatik çalışır. Bu pencereyi kapatabilirsiniz.\r\nYerel ağ istemcileri sunucuyu otomatik bulabilir.";
        }
        catch(Exception ex){if(!IsDisposed)state.Text="Server bilgileri alınamadı.\r\n\r\n"+ex.Message;}
        finally{refreshing=false;}
    }
}
