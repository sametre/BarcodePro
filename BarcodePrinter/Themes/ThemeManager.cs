namespace BarcodePrinter.Themes;

public sealed record AppTheme(Color Background, Color Surface, Color Foreground, Color Muted, Color Border, Color Accent, Color Selection);
public static class ThemeManager
{
    public static bool IsDark { get; private set; }
    public static AppTheme Current => IsDark
        ? new(Color.FromArgb(19,25,34), Color.FromArgb(28,36,47), Color.FromArgb(230,237,245), Color.FromArgb(156,173,192), Color.FromArgb(51,64,80), Color.FromArgb(83,158,242), Color.FromArgb(38,57,82))
        : new(Color.FromArgb(239,242,246), Color.White, Color.FromArgb(40,49,62), Color.FromArgb(101,114,132), Color.FromArgb(204,213,225), Color.FromArgb(41,104,181), Color.FromArgb(220,234,251));
    public static void Toggle() { IsDark = !IsDark; foreach (Form form in Application.OpenForms) Apply(form); }
    public static void Apply(Control root)
    {
        var t = Current;
        root.BackColor = root is Form || root.Tag as string == "canvas" ? t.Background : t.Surface; root.ForeColor = root.Tag as string == "accent" ? t.Accent : t.Foreground;
        if (root is Button b) { b.BackColor = b.Tag as string == "primary" ? t.Accent : t.Surface; b.ForeColor = b.Tag as string == "primary" ? Color.White : t.Foreground; }
        bool caption=false;for(Control? parent=root;parent!=null;parent=parent.Parent)if(parent.Tag as string=="caption"){caption=true;break;}
        if(caption){root.BackColor=IsDark?Color.FromArgb(32,48,68):Color.FromArgb(221,232,246);root.ForeColor=t.Foreground;}
        if (root is DataGridView g) { g.BackgroundColor = t.Surface; g.GridColor = t.Border; g.DefaultCellStyle.BackColor = t.Surface; g.DefaultCellStyle.ForeColor = t.Foreground; g.DefaultCellStyle.SelectionBackColor = t.Selection; g.DefaultCellStyle.SelectionForeColor = t.Foreground; g.ColumnHeadersDefaultCellStyle.BackColor = t.Background; g.ColumnHeadersDefaultCellStyle.ForeColor = t.Muted; g.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.Background; g.ColumnHeadersDefaultCellStyle.SelectionForeColor = t.Muted; }
        if (root is PropertyGrid p) { p.ViewBackColor = t.Surface; p.ViewForeColor = t.Foreground; p.HelpBackColor = t.Background; p.HelpForeColor = t.Foreground; p.LineColor = t.Border; }
        foreach (Control c in root.Controls) Apply(c);
        if (root is BarcodePrinter.Controls.Common.AppRibbon ribbon) ribbon.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppModuleMenu modules) modules.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppWorkspaceTabs tabs) tabs.RefreshTheme();
        root.Invalidate();
    }
}



