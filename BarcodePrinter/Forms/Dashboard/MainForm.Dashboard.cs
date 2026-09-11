using BarcodePrinter.Controls.Common;
using BarcodePrinter.Controls.Dashboard;
using BarcodePrinter.Themes;
namespace BarcodePrinter;
public partial class Form1
{
    private void Dashboard()
    {
        var d=inventory.Data;var today=d.Movements.Where(m=>m.At.Date==DateTime.Today).ToList();

        // Eğer hiçbir veri yoksa (boş state) - merkezi saydam logo göster
        if(d.Products.Count==0)
        {
            var emptyPanel=new Panel{Dock=DockStyle.Fill,Tag="canvas"};
            var logoImage=BrandAssets.CreateMark(256);
            emptyPanel.Paint+=(_,e)=>{
                if(logoImage==null)return;
                var x=(emptyPanel.Width-256)/2;
                var y=(emptyPanel.Height-256)/2;

                // Opacity effekti - mat gri üzerinde çiz
                using var brush=new SolidBrush(Color.FromArgb(100,44,44,44));
                e.Graphics.FillEllipse(brush,x-20,y-20,296,296);

                // Logo'yu %50 opacity'de çiz
                var cm=new System.Drawing.Imaging.ColorMatrix();
                cm.Matrix33=0.5f;
                using var attributes=new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(cm);
                e.Graphics.DrawImage(logoImage,new Rectangle(x,y,256,256),
                    0,0,256,256,GraphicsUnit.Pixel,attributes);

                // Alt metin
                using var font=AppTypography.Body();
                using var brushText=new SolidBrush(Muted);
                var text="Başlamak için ilk ürünü ekleyin";
                var size=e.Graphics.MeasureString(text,font);
                e.Graphics.DrawString(text,font,brushText,(emptyPanel.Width-size.Width)/2,y+280);
            };
            content.Controls.Add(emptyPanel);
            return;
        }

        var surface=new DashboardSurface();var layout=new TableLayoutPanel{ColumnCount=1,RowCount=4,Margin=Padding.Empty,Tag="canvas"};
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,50));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,150));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,145));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var header=new Panel{Dock=DockStyle.Fill,Tag="canvas",Margin=Padding.Empty};
        header.Controls.Add(new Label{Text="Genel bakış",Font=AppTypography.Heading(),AutoSize=true,Location=new Point(0,1),Tag="canvas"});
        header.Controls.Add(new Label{Text=DateTime.Today.ToString("dd MMMM yyyy, dddd")+"  ·  Envanter ve günlük hareketler",Font=AppTypography.Small(),AutoSize=true,Location=new Point(2,27),Tag="canvas"});
        var add=Button("Yeni ürün",()=>EditProduct(null));add.AutoSize=false;add.Size=new Size(108,27);header.Controls.Add(add);header.Resize+=(_,_)=>add.Location=new Point(header.Width-116,8);
        var cards=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=2,Margin=Padding.Empty,Tag="canvas"};for(int i=0;i<4;i++)cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,25));for(int i=0;i<2;i++)cards.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        (string Caption,string Value,AppIcon Icon,bool Warning)[] metrics=[
            ("Toplam ürün",d.Products.Count.ToString("N0"),AppIcon.Products,false),
            ("Toplam stok¹",d.Products.Sum(p=>p.Stock).ToString("N0"),AppIcon.Stock,false),
            ("Kritik stok",d.Products.Count(p=>p.Stock>0&&p.Stock<=p.Minimum).ToString(),AppIcon.Bell,true),
            ("Stokta olmayan",d.Products.Count(p=>p.Stock==0).ToString(),AppIcon.Barcode,true),
            ("Bugünkü giriş¹",today.Where(m=>m.Delta>0).Sum(m=>m.Delta).ToString("N0"),AppIcon.Add,false),
            ("Bugünkü çıkış¹",(-today.Where(m=>m.Delta<0).Sum(m=>m.Delta)).ToString("N0"),AppIcon.Export,false),
            ("Aktif ürün",d.Products.Count(p=>p.Active).ToString(),AppIcon.Products,false),
            ("Pasif ürün",d.Products.Count(p=>!p.Active).ToString(),AppIcon.History,false)];
        foreach(var m in metrics)cards.Controls.Add(new MetricCard{Caption=m.Caption,Value=m.Value,IconKind=m.Icon,Warning=m.Warning});
        var chart=new StockChart(d.Movements){Dock=DockStyle.Fill,Margin=new Padding(0,0,10,12)};
        var lower=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty,Tag="canvas"};lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68));lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32));
        var recent=Grid();recent.DataSource=d.Movements.OrderByDescending(m=>m.At).Take(12).Select(m=>new{Tarih=m.At.ToString("dd.MM HH:mm"),Ürün=m.ProductName,İşlem=m.Kind,Miktar=m.Delta.ToString("+0.##;-0.##;0"),Açıklama=m.Note}).ToList();
        var recentSection=new AppSection("Son işlemler");recentSection.Body.Padding=new Padding(10,4,10,8);recentSection.Body.Controls.Add(recent);
        if(d.Movements.Count==0)recentSection.Body.Controls.Add(Empty("Henüz bir stok hareketi yok.\nStok menüsünden ilk işleminizi ekleyebilirsiniz."));
        var critical=Grid();critical.DataSource=d.Products.Where(p=>p.Stock<=p.Minimum).OrderBy(p=>p.Stock).Select(p=>new{Ürün=p.Name,Stok=p.Stock,Minimum=p.Minimum}).ToList();
        var criticalSection=new AppSection("Stok uyarıları");criticalSection.Body.Padding=new Padding(10,4,10,8);criticalSection.Body.Controls.Add(critical);
        if(!d.Products.Any(p=>p.Stock<=p.Minimum))criticalSection.Body.Controls.Add(Empty("Kritik stok uyarısı yok.\nEnvanterinizdeki değişiklikler burada görünür."));
        lower.Controls.Add(recentSection,0,0);lower.Controls.Add(criticalSection,1,0);
        layout.Controls.Add(header,0,0);layout.Controls.Add(cards,0,1);layout.Controls.Add(chart,0,2);layout.Controls.Add(lower,0,3);surface.Controls.Add(layout);content.Controls.Add(surface);
    }
    private static Label Empty(string text)=>new(){Text=text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,Font=AppTypography.Body(),ForeColor=Muted,Padding=new Padding(16)};
}
