using System.Globalization;
using BarcodePrinter.Forms.Network;
using BarcodePrinter.Services.Network;
using BarcodePrinter.Services.Licensing;

namespace BarcodePrinter;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length>0&&args[0]=="--set-printer-port"){Environment.ExitCode=Services.Printing.PrinterRouting.RunPortHelper(args);return;}
        string? Argument(string name){var index=Array.FindIndex(args,a=>a.Equals(name,StringComparison.OrdinalIgnoreCase));return index>=0&&index+1<args.Length?args[index+1]:null;}
        if(args.Contains("--service",StringComparer.OrdinalIgnoreCase))
        {
            if (LicenseService.Validate(true) is string licenseError) { WriteServiceError(Argument("--data-dir"), new InvalidOperationException("License: " + licenseError)); Environment.ExitCode = 2; return; }
            try{LanInventoryServer.RunServiceAsync(Argument("--data-dir"),Argument("--service-name")??ServerConfiguration.ServiceName).GetAwaiter().GetResult();}
            catch(Exception ex){WriteServiceError(Argument("--data-dir"),ex);Environment.ExitCode=1;}
            return;
        }
        if(args.Contains("--initialize-server",StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var directory=Argument("--data-dir")??ServerConfiguration.DataDirectory;
                var configuration=ServerConfiguration.Load(directory,create:true);
                if(Argument("--port") is string port){if(!int.TryParse(port,out int number)||number is <1024 or >65535)throw new InvalidOperationException("Geçersiz port.");configuration.Port=number;configuration.Save(directory);}
                var source=Argument("--source-data")??Path.Combine(Helpers.JsonStore.Root,"inventory.db");
                if(!File.Exists(source)&&File.Exists(Path.ChangeExtension(source,".json")))source=Path.ChangeExtension(source,".json");
                if(!File.Exists(source))source=Argument("--seed-data")??source;
                Services.Network.ServerDataMigration.Migrate(source,directory);
            }
            catch(Exception ex){WriteServiceError(Argument("--data-dir"),ex);Environment.ExitCode=1;}
            return;
        }
        var mode=args.Contains("--server",StringComparer.OrdinalIgnoreCase)?"Server":args.Contains("--client",StringComparer.OrdinalIgnoreCase)?"Client":"Inventory";
        using var instance = new Mutex(true, "Local\\BarcodePro."+mode, out bool first);
        if (!first) { MessageBox.Show("R3 M-Kobi zaten çalışıyor."); return; }
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => { Helpers.JsonStore.Log(e.Exception); MessageBox.Show(e.Exception.Message, "R3 M-Kobi — İşlem hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); };
        try
        {
            var serverLicense = args.Contains("--server", StringComparer.OrdinalIgnoreCase) || args.Contains("--server-admin", StringComparer.OrdinalIgnoreCase);
            if (!EnsureLicense(serverLicense)) return;
            if(args.Contains("--server",StringComparer.OrdinalIgnoreCase)){Application.Run(new ServerStatusForm());return;}
            using var login=new LoginForm();if(login.ShowDialog()!=DialogResult.OK)return;
            new Services.Templates.TemplateService().InstallPresets();
            if(args.Contains("--client",StringComparer.OrdinalIgnoreCase))
            {
                using var setup=new ClientConnectionForm();if(setup.ShowDialog()!=DialogResult.OK)return;
                using var remote=new RemoteInventoryClient(setup.Settings!);Application.Run(new Form1(remote:remote));return;
            }
            Application.Run(new Form1(inventoryPath:args.Contains("--server-admin",StringComparer.OrdinalIgnoreCase)?ServerConfiguration.InventoryPath:null));
        }
        catch (Exception ex) { MessageBox.Show("Uygulama başlatılamadı. Mevcut veriler korunmuştur. SQLite veritabanını, eski JSON migration yedeğini ve Server bağlantısını kontrol edin.\n\n" + ex.Message, "R3 M-Kobi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private static bool EnsureLicense(bool server)
    {
        if (LicenseService.Validate(server) is null) return true;
        using var activation = new LicenseActivationForm(server);
        return activation.ShowDialog() == DialogResult.OK && LicenseService.Validate(server) is null;
    }
    private static void WriteServiceError(string? directory,Exception error)
    {
        try{var path=directory??ServerConfiguration.DataDirectory;Directory.CreateDirectory(path);File.AppendAllText(Path.Combine(path,"service-error.log"),DateTimeOffset.Now.ToString("O")+" "+error+Environment.NewLine);}
        catch(IOException){}catch(UnauthorizedAccessException){}
    }
}

