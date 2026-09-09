using System.Drawing.Drawing2D;
using BarcodePrinter.Controls.Common;
using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Dashboard;
public sealed class MetricCard : Control
{
    public string Caption {get;set;}="";public string Value {get;set;}="0";public AppIcon IconKind{get;set;}=AppIcon.Products;public bool Warning{get;set;}
    public MetricCard(){DoubleBuffered=true;Dock=DockStyle.Fill;Margin=new Padding(0,0,10,10);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var t=ThemeManager.Current;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(t.Background);
        var r=new RectangleF(.5f,.5f,Width-2,Height-2);using var shape=new GraphicsPath();const int round=12;shape.AddArc(r.X,r.Y,round,round,180,90);shape.AddArc(r.Right-round,r.Y,round,round,270,90);shape.AddArc(r.Right-round,r.Bottom-round,round,round,0,90);shape.AddArc(r.X,r.Bottom-round,round,round,90,90);shape.CloseFigure();using var fill=new SolidBrush(t.Surface);using var border=new Pen(t.Border);g.FillPath(fill,shape);g.DrawPath(border,shape);
        using var small=AppTypography.Small();using var large=new Font("Segoe UI",22,FontStyle.Bold);var accent=Warning?Color.FromArgb(196,114,39):t.Accent;
        TextRenderer.DrawText(g,Caption,small,new Rectangle(16,12,Math.Max(1,Width-68),20),t.Muted,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g,Value,large,new Rectangle(13,33,Math.Max(1,Width-66),36),t.Foreground,TextFormatFlags.Left|TextFormatFlags.EndEllipsis);
        using var icon=AppIcons.Create(IconKind,accent,22);g.DrawImageUnscaled(icon,Width-42,27);
    }
}
public sealed class DashboardSurface : Panel
{
    public DashboardSurface(){Dock=DockStyle.Fill;Tag="canvas";DoubleBuffered=true;}
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(Controls.Count>0){int width=Math.Min(1480,ClientSize.Width);Controls[0].Bounds=new Rectangle((ClientSize.Width-width)/2,0,width,ClientSize.Height);}}
}
