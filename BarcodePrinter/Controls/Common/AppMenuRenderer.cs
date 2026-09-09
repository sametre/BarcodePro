using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;

public sealed class AppMenuRenderer : ToolStripProfessionalRenderer
{
    public AppMenuRenderer():base(new Colors()){RoundedEdges=false;}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen=new Pen(ThemeManager.Current.Border);
        if(e.ToolStrip is ToolStripDropDown)e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);
        else e.Graphics.DrawLine(pen,0,e.ToolStrip.Height-1,e.ToolStrip.Width,e.ToolStrip.Height-1);
    }
    private sealed class Colors:ProfessionalColorTable
    {
        public override Color MenuItemSelected=>ThemeManager.Current.Selection;
        public override Color MenuItemBorder=>ThemeManager.Current.Border;
        public override Color ButtonSelectedHighlight=>ThemeManager.Current.Selection;
        public override Color ButtonSelectedGradientBegin=>ThemeManager.Current.Selection;
        public override Color ButtonSelectedGradientMiddle=>ThemeManager.Current.Selection;
        public override Color ButtonSelectedGradientEnd=>ThemeManager.Current.Selection;
        public override Color MenuItemSelectedGradientBegin=>ThemeManager.Current.Selection;
        public override Color MenuItemSelectedGradientEnd=>ThemeManager.Current.Selection;
        public override Color MenuItemPressedGradientBegin=>ThemeManager.Current.Selection;
        public override Color MenuItemPressedGradientMiddle=>ThemeManager.Current.Selection;
        public override Color MenuItemPressedGradientEnd=>ThemeManager.Current.Selection;
        public override Color ImageMarginGradientBegin=>ThemeManager.Current.Background;
        public override Color ImageMarginGradientMiddle=>ThemeManager.Current.Background;
        public override Color ImageMarginGradientEnd=>ThemeManager.Current.Background;
        public override Color ToolStripDropDownBackground=>ThemeManager.Current.Surface;
    }
}
