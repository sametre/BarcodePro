namespace BarcodePrinter;
public partial class Form1 {
    private void Settings()
    {
        var flow = Vertical(); flow.Controls.Add(Label("Çalışma alanı", 14, true));
        flow.Controls.Add(Label(clientMode?"Ürün ve stok kayıtları Barcode Pro Server üzerindeki ortak SQLite 3 veritabanında saklanır.":"Ürünler, stok hareketleri ve MySQL aktarımları transaction destekli SQLite 3 veritabanında saklanır."));
        var path = new TextBox { Text = inventory.DatabasePath, ReadOnly = true, Width = 740 }; flow.Controls.Add(path); flow.Controls.Add(Label(""));
        flow.Controls.Add(Button("Yazıcı ayarları ve kalibrasyon", () => { using var f = new BarcodePrinter.Forms.Printers.PrintersForm(); f.ShowDialog(this); UpdateConnection(); })); flow.Controls.Add(Label(""));
        flow.Controls.Add(Button("Veri yedeğini dışa aktar", () => Attempt(() => { using var save = new SaveFileDialog { Filter = "JSON yedeği|*.json", FileName = $"barcode-pro-{DateTime.Now:yyyyMMdd-HHmm}.json" }; if (save.ShowDialog(this) == DialogResult.OK) File.WriteAllText(save.FileName, System.Text.Json.JsonSerializer.Serialize(inventory.Data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); })));
        flow.Controls.Add(Label("")); flow.Controls.Add(Label("Denemek için örnek envanter", 11, true)); flow.Controls.Add(Label("Yalnızca boş çalışma alanına, açıkça işaretlenmiş 8 örnek ürün ekler."));
        flow.Controls.Add(Button("Örnek ürünleri yükle", () => Attempt(() => { inventory.SeedDemo(); ShowPage("Genel bakış"); }), false));
        flow.Controls.Add(Label("")); flow.Controls.Add(Label("Stok düzeltme, girilen değeri yeni toplam stok olarak kaydeder.\nİade stoğu artırır; fire ve çıkış stoğu azaltır.\nMinimum stok uyarı eşiğidir. Maksimum stok planlama sınırıdır.\nServer ve Client işlemleri ortak envanter üzerinde çalışır."));
        var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new Point(12,5),Font=BarcodePrinter.Themes.AppTypography.Body()};
        var sql=new TabPage("MySQL içe aktar");sql.Controls.Add(new MySqlConnectionPanel(inventory,Path.Combine(BarcodePrinter.Helpers.JsonStore.Root,"mysql-connection.json")));
        var local=new TabPage("SQLite veri ve yazıcı"){AutoScroll=true,Padding=new Padding(12)};
        var company=new TabPage("Firma ve etiket");company.Controls.Add(new CompanySettingsPanel(Path.Combine(BarcodePrinter.Helpers.JsonStore.Root,"company.json"),Path.Combine(BarcodePrinter.Helpers.JsonStore.Root,"templates")));
        content.Controls.Remove(flow);local.Controls.Add(flow);
        if(!clientMode){var fileImport=new TabPage("SQL dosyasından ürün aktar");fileImport.Controls.Add(new SqlFileImportPanel(inventory));tabs.TabPages.Add(fileImport);tabs.TabPages.Add(sql);}
        tabs.TabPages.AddRange([company,local]);content.Controls.Add(tabs);
    }

}
