using System.Text;
namespace BarcodePrinter;
public partial class Form1
{
    private void Movements()
    {
        var grid = Grid(); var bar = new BarcodePrinter.Controls.Common.AppToolbar();
        var search = new TextBox { Width = 260, PlaceholderText = "Ürün veya açıklama ara…" };
        var from = new DateTimePicker { Width = 145, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        var to = new DateTimePicker { Width = 145, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        List<Movement> Visible() => inventory.Data.Movements.Where(m => m.At.Date >= from.Value.Date && m.At.Date <= to.Value.Date && (m.ProductName + " " + m.Note).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).OrderByDescending(m => m.At).ToList();
        void Refresh() => grid.DataSource = Visible().Select(m => new { Tarih = m.At.ToString("dd.MM.yyyy HH:mm"), Ürün = m.ProductName, İşlem = m.Kind, Önce = m.Before, Değişim = m.Delta, Sonra = m.After, Açıklama = m.Note }).ToList();
        bar.Controls.Add(search); bar.Controls.Add(from); bar.Controls.Add(to); bar.Controls.Add(Button("CSV indir", () => Attempt(() => Export(Visible())), false));
        search.TextChanged += (_, _) => Refresh(); from.ValueChanged += (_, _) => Refresh(); to.ValueChanged += (_, _) => Refresh();
        content.Controls.Add(grid); content.Controls.Add(bar); Refresh();
    }
    private void Export(List<Movement> movements)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV dosyası|*.csv", FileName = $"stok-hareketleri-{DateTime.Today:yyyyMMdd}.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        static string Escape(string s) { if (s.Length > 0 && "=+-@\t\r".Contains(s[0])) s = "'" + s; return "\"" + s.Replace("\"", "\"\"") + "\""; }
        var lines = new List<string> { "Tarih;Ürün;İşlem;Önce;Değişim;Sonra;Açıklama" };
        lines.AddRange(movements.Select(m => string.Join(";", new[] { m.At.ToString("O"), m.ProductName, m.Kind, m.Before.ToString(), m.Delta.ToString(), m.After.ToString(), m.Note }.Select(Escape))));
        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
    }

}

