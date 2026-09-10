using System.Net;
using System.Net.Sockets;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Themes;

namespace BarcodePrinter.Forms.Network;

public sealed class ServerStatusForm : AppWindow
{
    private LanInventoryServer? server;private readonly Label state=new(){Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter};
    public ServerStatusForm(){Text="Barcode Pro Server";ClientSize=new Size(600,360);StartPosition=FormStartPosition.CenterScreen;Controls.Add(state);Shown+=async(_,_)=>await Start();FormClosed+=async(_,_)=>{if(server!=null)await server.DisposeAsync();};ThemeManager.Apply(this);}
    private async Task Start()
    {
        try
        {
            server=await LanInventoryServer.StartAsync();var ips=Dns.GetHostAddresses(Dns.GetHostName()).Where(x=>x.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(x)).Select(x=>$"http://{x}:5088");
            state.Text="BARCODE PRO SERVER ÇALIŞIYOR\n\nClient bağlantı adresleri:\n"+string.Join("\n",ips)+"\n\nErişim anahtarı: owner\n\nBu pencere açık kaldığı sürece istemciler bağlanabilir.";
        }
        catch(Exception ex){state.Text="Server başlatılamadı.\n\n"+ex.Message;state.ForeColor=Color.Firebrick;}
    }
}
