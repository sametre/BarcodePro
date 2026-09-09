using System.Text;
namespace BarcodePrinter;
public partial class Form1
{
    private void Barcode()
    {
        var flow = Vertical(); flow.Controls.Add(Label("Okutun. Bulun. İşlem yapın.", 23, true));
        flow.Controls.Add(Label("USB / Bluetooth barkod okuyucunuzu kullanın veya barkodu yazıp Enter'a basın."));
        var box = new TextBox { Width = 620, Font = new Font("Segoe UI", 22), PlaceholderText = "Barkod numarası", Margin = new Padding(0, 20, 0, 20), MaxLength = 64 };
        var result = new FlowLayoutPanel { Width = 760, Height = 270, FlowDirection = FlowDirection.TopDown, Padding = new Padding(24), BackColor = Color.White };
        void Find()
        {
            result.Controls.Clear(); var barcode = box.Text.Trim();
            if (!Inventory.ValidBarcode(barcode)) { result.Controls.Add(Label("Geçersiz barkod biçimi.", 14, true)); return; }
            var p = inventory.Data.Products.FirstOrDefault(p => p.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase));
            if (p == null) { result.Controls.Add(Label("Bu barkoda ait ürün bulunamadı.", 14, true)); result.Controls.Add(Label("Ürünler ekranından barkodu kontrol edin veya yeni ürün ekleyin.")); return; }
            result.Controls.Add(Label(p.Name, 22, true)); result.Controls.Add(Label($"{p.Sku}  /  {p.Category}  /  {p.Barcode}"));
            result.Controls.Add(Label($"{p.Stock:0.###} {p.Unit}     •     {p.Price:C2}", 18, true));
            result.Controls.Add(Label($"{(p.Active ? "Aktif" : "Pasif")}  •  {(p.OnMenu ? "Menüde" : "Menü dışında")}  •  {(p.Stock <= p.Minimum ? "Stok uyarısı" : "Stok yeterli")}"));
            result.Controls.Add(Button("Stok giriş / çıkış işlemi", () => StockDialog(p))); box.SelectAll(); box.Focus();
        }
        box.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Find(); } };
        flow.Controls.Add(box); flow.Controls.Add(Button("Ürünü bul  →", Find)); flow.Controls.Add(Label("")); flow.Controls.Add(result);
        flow.Controls.Add(Label("Kamera / mobil tarama bu masaüstü sürümünde desteklenmiyor.", 10));
        BeginInvoke(() => box.Focus());
    }

}
