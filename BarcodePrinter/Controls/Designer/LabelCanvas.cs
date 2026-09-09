using System.Drawing.Drawing2D;
using BarcodePrinter.Models.LabelDesigner;
using BarcodePrinter.Printing.Rendering;
using BarcodePrinter.Services.Templates;
namespace BarcodePrinter.Controls.Designer;
public sealed class LabelCanvas : Control
{
    public LabelTemplate Template {get;set;}=TemplateService.Standard();
    public Product PreviewProduct {get;set;}=DynamicFields.Sample;
    public List<Guid> Selection {get;}=[];
    public double GridMm {get;set;}=1;
    public bool SnapToGrid {get;set;}=true;
    public double Zoom {get;set;}=1;
    public event Action? SelectionChanged;
    public event Action? BeginEdit;
    public event Action? Edited;
    public event Action<string,PointF>? ToolDropped;
    private Point start;private bool dragging,resizing;
    private Dictionary<Guid,(double X,double Y,double W,double H)> original=[];
    private readonly LabelPreviewRenderer renderer=new();
    public LabelCanvas() {DoubleBuffered=true;Dock=DockStyle.Fill;AllowDrop=true;TabStop=true;ResizeRedraw=true;BackColor=Color.FromArgb(222,229,235);}
    public double PixelsPerMm=>Math.Max(.15,Math.Min((ClientSize.Width-100)/Template.WidthMm,(ClientSize.Height-100)/Template.HeightMm)*Zoom);
    public RectangleF Paper=>new(44,40,(float)(Template.WidthMm*PixelsPerMm),(float)(Template.HeightMm*PixelsPerMm));
    public IEnumerable<LabelElement> Selected=>Template.Elements.Where(e=>Selection.Contains(e.Id));
    public void SelectAll(){Selection.Clear();Selection.AddRange(Template.Elements.Select(e=>e.Id));SelectionChanged?.Invoke();Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.Clear(BackColor);var paper=Paper;
        using var shadow=new SolidBrush(Color.FromArgb(190,199,207));g.FillRectangle(shadow,paper.X+5,paper.Y+5,paper.Width,paper.Height);g.FillRectangle(Brushes.White,paper);
        var state=g.Save();g.TranslateTransform(paper.X,paper.Y);g.SetClip(new RectangleF(0,0,paper.Width,paper.Height));using (var raster = renderer.Render(Template,PreviewProduct,Template.Dpi,DateTime.Now,false)) { g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.DrawImage(raster,new RectangleF(0,0,paper.Width,paper.Height)); }
        using var grid=new Pen(Color.FromArgb(34,80,100,120));
        if(GridMm*PixelsPerMm>=4) {for(double x=0;x<=Template.WidthMm;x+=GridMm)g.DrawLine(grid,(float)(x*PixelsPerMm),0,(float)(x*PixelsPerMm),paper.Height);for(double y=0;y<=Template.HeightMm;y+=GridMm)g.DrawLine(grid,0,(float)(y*PixelsPerMm),paper.Width,(float)(y*PixelsPerMm));}
        using var selected=new Pen(Color.FromArgb(0,145,117),2) {DashStyle=DashStyle.Dash};
        foreach(var item in Selected) {var r=new RectangleF((float)(item.Xmm*PixelsPerMm),(float)(item.Ymm*PixelsPerMm),(float)(item.WidthMm*PixelsPerMm),(float)(item.HeightMm*PixelsPerMm));g.DrawRectangle(selected,r.X,r.Y,r.Width,r.Height);g.FillRectangle(Brushes.Teal,r.Right-5,r.Bottom-5,10,10);}
        g.Restore(state);using var font=new Font("Segoe UI",8);using var pen=new Pen(Color.SlateGray);
        for(int mm=0;mm<=Template.WidthMm;mm+=5) {float x=paper.X+(float)(mm*PixelsPerMm);g.DrawLine(pen,x,26,x,36);g.DrawString(mm.ToString(),font,Brushes.SlateGray,x+2,10);}
        for(int mm=0;mm<=Template.HeightMm;mm+=5) {float y=paper.Y+(float)(mm*PixelsPerMm);g.DrawLine(pen,30,y,40,y);g.DrawString(mm.ToString(),font,Brushes.SlateGray,3,y);}
        g.DrawString("mm",font,Brushes.SlateGray,4,10);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);Focus();if(e.Button is not MouseButtons.Left and not MouseButtons.Right)return;
        double x=(e.X-Paper.X)/PixelsPerMm,y=(e.Y-Paper.Y)/PixelsPerMm;
        var hit=Template.Elements.Where(i=>i.IsVisible && x>=i.Xmm && x<=i.Xmm+i.WidthMm && y>=i.Ymm && y<=i.Ymm+i.HeightMm).OrderByDescending(i=>i.ZIndex).FirstOrDefault();
        if(hit==null) {Selection.Clear();SelectionChanged?.Invoke();Invalidate();return;}
        if((ModifierKeys&Keys.Control)!=0) {if(!Selection.Remove(hit.Id))Selection.Add(hit.Id);} else if(!Selection.Contains(hit.Id)){Selection.Clear();Selection.Add(hit.Id);}
        SelectionChanged?.Invoke();if(e.Button==MouseButtons.Right){Invalidate();return;}
        start=e.Location;resizing=Selection.Count==1 && Math.Abs(x-hit.Xmm-hit.WidthMm)*PixelsPerMm<9 && Math.Abs(y-hit.Ymm-hit.HeightMm)*PixelsPerMm<9;
        original=Selected.ToDictionary(i=>i.Id,i=>(i.Xmm,i.Ymm,i.WidthMm,i.HeightMm));dragging=Selection.Count>0;Capture=dragging;if(dragging)BeginEdit?.Invoke();Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(!dragging)return;
        double dx=(e.X-start.X)/PixelsPerMm,dy=(e.Y-start.Y)/PixelsPerMm;
        if(SnapToGrid){dx=Math.Round(dx/GridMm)*GridMm;dy=Math.Round(dy/GridMm)*GridMm;}
        if(!resizing && original.Count>0) {dx=Math.Clamp(dx,-original.Values.Min(v=>v.X),Template.WidthMm-original.Values.Max(v=>v.X+v.W));dy=Math.Clamp(dy,-original.Values.Min(v=>v.Y),Template.HeightMm-original.Values.Max(v=>v.Y+v.H));}
        foreach(var item in Selected) {var old=original[item.Id];if(resizing){item.WidthMm=Math.Clamp(old.W+dx,.5,Template.WidthMm-item.Xmm);item.HeightMm=Math.Clamp(old.H+dy,.5,Template.HeightMm-item.Ymm);}else {item.Xmm=old.X+dx;item.Ymm=old.Y+dy;}}
        Invalidate();
    }
    protected override void OnMouseUp(MouseEventArgs e) {base.OnMouseUp(e);if(dragging){dragging=false;Capture=false;Edited?.Invoke();}}
    protected override void OnDragEnter(DragEventArgs drgevent) {base.OnDragEnter(drgevent);drgevent.Effect=drgevent.Data?.GetDataPresent(DataFormats.Text)==true?DragDropEffects.Copy:DragDropEffects.None;}
    protected override void OnDragDrop(DragEventArgs e) {base.OnDragDrop(e);if(e.Data?.GetData(DataFormats.Text) is string tool){var p=PointToClient(new Point(e.X,e.Y));ToolDropped?.Invoke(tool,new PointF((float)((p.X-Paper.X)/PixelsPerMm),(float)((p.Y-Paper.Y)/PixelsPerMm)));}}
}



