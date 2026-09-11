using System.Drawing.Drawing2D;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Dashboard;
public sealed class MetricCard : Control
{
    public string Caption {get;set;}="";public string Value {get;set;}="0";public AppIcon IconKind{get;set;}=AppIcon.Products;public bool Warning{get;set;}
    public MetricCard(){DoubleBuffered=true;Dock=DockStyle.Fill;Margin=new Padding(0,0,8,8);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var t=ThemeManager.Current;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(t.Background);
        var r=new RectangleF(.5f,.5f,Width-2,Height-2);using var fill=new SolidBrush(t.Surface);using var border=new Pen(t.Border);g.FillRectangle(fill,r);g.DrawRectangle(border,r.X,r.Y,r.Width,r.Height);
        using var small=AppTypography.Small();using var large=new Font("Segoe UI",19,FontStyle.Bold);var accent=Warning?Color.FromArgb(196,114,39):t.Accent;
        TextRenderer.DrawText(g,Caption,small,new Rectangle(10,7,Math.Max(1,Width-58),18),t.Muted,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g,Value,large,new Rectangle(8,27,Math.Max(1,Width-58),30),t.Foreground,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
        using var icon=AppIcons.Create(IconKind,accent,18);g.DrawImageUnscaled(icon,Width-34,24);
    }
}
public sealed class DashboardSurface : Panel
{
    public DashboardSurface(){Dock=DockStyle.Fill;Tag="canvas";DoubleBuffered=true;}
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(Controls.Count>0){int width=Math.Min(1480,ClientSize.Width);Controls[0].Bounds=new Rectangle((ClientSize.Width-width)/2,0,width,ClientSize.Height);}}
}
