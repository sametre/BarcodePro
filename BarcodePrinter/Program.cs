using System.Globalization;
using BarcodePrinter.Forms.Network;
using BarcodePrinter.Services.Network;

namespace BarcodePrinter;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length>0&&args[0]=="--set-printer-port"){Environment.ExitCode=Services.Printing.PrinterRouting.RunPortHelper(args);return;}
        var mode=args.Contains("--server",StringComparer.OrdinalIgnoreCase)?"Server":args.Contains("--client",StringComparer.OrdinalIgnoreCase)?"Client":"Inventory";
        using var instance = new Mutex(true, "Local\\BarcodePro."+mode, out bool first);
        if (!first) { MessageBox.Show("Barcode Pro zaten çalışıyor."); return; }
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => { Helpers.JsonStore.Log(e.Exception); MessageBox.Show(e.Exception.Message, "Barcode Pro — İşlem hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); };
        try
        {
            if(args.Contains("--server",StringComparer.OrdinalIgnoreCase)){Application.Run(new ServerStatusForm());return;}
            using var login=new LoginForm();if(login.ShowDialog()!=DialogResult.OK)return;
            new Services.Templates.TemplateService().InstallPresets();
            if(args.Contains("--client",StringComparer.OrdinalIgnoreCase))
            {
                using var setup=new ClientConnectionForm();if(setup.ShowDialog()!=DialogResult.OK)return;
                using var remote=new RemoteInventoryClient(setup.Settings!);Application.Run(new Form1(remote:remote));return;
            }
            Application.Run(new Form1());
        }
        catch (Exception ex) { MessageBox.Show("Uygulama başlatılamadı. Mevcut veriler korunmuştur. Veri dosyasını ve .bak yedeğini kontrol edin.\n\n" + ex.Message, "Barcode Pro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

