using System.Globalization;

namespace BarcodePrinter;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var instance = new Mutex(true, "Local\\BarcodePro.Inventory", out bool first);
        if (!first) { MessageBox.Show("Barcode Pro zaten çalışıyor."); return; }
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => { Helpers.JsonStore.Log(e.Exception); MessageBox.Show(e.Exception.Message, "Barcode Pro — İşlem hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning); };
        try { using var login=new LoginForm();if(login.ShowDialog()!=DialogResult.OK)return;new Services.Templates.TemplateService().InstallPresets();Application.Run(new Form1()); }
        catch (Exception ex) { MessageBox.Show("Uygulama başlatılamadı. Mevcut veriler korunmuştur. Veri dosyasını ve .bak yedeğini kontrol edin.\n\n" + ex.Message, "Barcode Pro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

