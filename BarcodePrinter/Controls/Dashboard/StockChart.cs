using BarcodePrinter.Themes;
namespace BarcodePrinter;
internal sealed class StockChart : Control
{
    private readonly List<Movement> movements;
    public StockChart():this([]){}
    public StockChart(List<Movement> movements){this.movements=movements;DoubleBuffered=true;ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var t=ThemeManager.Current;var g=e.Graphics;g.Clear(t.Surface);using var border=new Pen(t.Border);g.DrawRectangle(border,0,0,Width-1,Height-1);
        using var title=new Font("Segoe UI",10,FontStyle.Bold);using var small=AppTypography.Small();using var ink=new SolidBrush(t.Foreground);using var muted=new SolidBrush(t.Muted);using var incoming=new SolidBrush(t.Accent);using var outgoing=new SolidBrush(Color.FromArgb(221,158,91));
        g.DrawString("Son 7 gün",title,ink,16,12);g.DrawString("¹ Miktarlar farklı birimlerin sayısal toplamıdır.",small,muted,115,14);g.DrawString("■ Giriş",small,incoming,Width-155,14);g.DrawString("■ Çıkış",small,outgoing,Width-82,14);
        var days=Enumerable.Range(0,7).Select(i=>DateTime.Today.AddDays(i-6)).ToArray();var values=days.Select(d=>(In:movements.Where(m=>m.At.Date==d&&m.Delta>0).Sum(m=>m.Delta),Out:-movements.Where(m=>m.At.Date==d&&m.Delta<0).Sum(m=>m.Delta))).ToArray();
        var max=Math.Max(1,values.Max(v=>Math.Max(v.In,v.Out)));float baseline=Height-30,top=47,step=(Width-48)/7f;
        using var grid=new Pen(t.Border);g.DrawLine(grid,24,baseline,Width-24,baseline);
        if(values.All(v=>v.In==0&&v.Out==0))TextRenderer.DrawText(g,"Bu hafta henüz stok hareketi yok.",small,new Rectangle(0,50,Width,Math.Max(20,Height-85)),t.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        for(int i=0;i<7;i++){float x=24+i*step+step/2-15;float a=(float)(values[i].In/max)*(baseline-top),b=(float)(values[i].Out/max)*(baseline-top);if(a>0)g.FillRectangle(incoming,x,baseline-a,12,a);if(b>0)g.FillRectangle(outgoing,x+17,baseline-b,12,b);g.DrawString(days[i].ToString("dd MMM"),small,muted,x-4,baseline+7);}
    }
}

