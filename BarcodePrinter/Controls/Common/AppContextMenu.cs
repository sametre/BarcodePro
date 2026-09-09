using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;
public sealed class AppContextMenu : ContextMenuStrip
{
    private readonly List<Image> icons=[];
    public AppContextMenu(){Font=new Font("Segoe UI",9);ShowImageMargin=true;Renderer=new ToolStripProfessionalRenderer(new MenuColors());}
    protected override void OnOpening(System.ComponentModel.CancelEventArgs e)
    {
        foreach(var icon in icons)icon.Dispose();icons.Clear();BackColor=ThemeManager.Current.Surface;ForeColor=ThemeManager.Current.Foreground;
        foreach(ToolStripItem item in Items){item.ForeColor=ForeColor;item.BackColor=BackColor;if(item is ToolStripMenuItem){var image=AppIcons.Create(AppIcons.ForText(item.Text??""),ForeColor,16);icons.Add(image);item.Image=image;}}
        base.OnOpening(e);
    }
    protected override void Dispose(bool disposing){if(disposing){foreach(var icon in icons)icon.Dispose();icons.Clear();}base.Dispose(disposing);}
    private sealed class MenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected=>ThemeManager.Current.Selection;
        public override Color MenuItemBorder=>ThemeManager.Current.Border;
        public override Color ToolStripDropDownBackground=>ThemeManager.Current.Surface;
        public override Color ImageMarginGradientBegin=>ThemeManager.Current.Surface;
        public override Color ImageMarginGradientMiddle=>ThemeManager.Current.Surface;
        public override Color ImageMarginGradientEnd=>ThemeManager.Current.Surface;
    }
}
