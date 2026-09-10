using System.Net;
using System.Net.Sockets;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Themes;

namespace BarcodePrinter.Forms.Network;

public sealed class ServerStatusForm : AppWindow
{
    private LanInventoryServer? server;private readonly AppTextBox state=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,TextAlign=HorizontalAlignment.Center};
    public ServerStatusForm()
    {
        Text="Barcode Pro Server";ClientSize=new Size(640,410);StartPosition=FormStartPosition.CenterScreen;
        var copy=new AppButton{Text="Bağlantı bilgilerini kopyala",IconKind=AppIcon.Export,Dock=DockStyle.Bottom,Height=42};copy.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(state.Text))Clipboard.SetText(state.Text);};
        var panel=new Panel{Dock=DockStyle.Fill,Padding=new Padding(34)};panel.Controls.Add(state);panel.Controls.Add(copy);Controls.Add(panel);
        Shown+=async(_,_)=>await Start();FormClosed+=async(_,_)=>{if(server!=null)await server.DisposeAsync();};ThemeManager.Apply(this);
    }
    private async Task Start()
    {
        try
        {
            server=await LanInventoryServer.StartAsync();var ips=Dns.GetHostAddresses(Dns.GetHostName()).Where(x=>x.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(x)).Select(x=>$"http://{x}:5088");
            state.Text="BARCODE PRO SERVER ÇALIŞIYOR\r\n\r\nBilgisayar: "+Environment.MachineName+"\r\nPort: 5088\r\n\r\nCLIENT BAĞLANTI ADRESLERİ\r\n"+string.Join("\r\n",ips)+"\r\n\r\nErişim anahtarı: owner\r\n\r\nGüvenlik duvarı ve otomatik başlangıç Server kurulumu tarafından ayarlanır. Bu pencere açık kaldığı sürece istemciler bağlanabilir.";
        }
        catch(Exception ex){state.Text="Server başlatılamadı.\n\n"+ex.Message;state.ForeColor=Color.Firebrick;}
    }
}
