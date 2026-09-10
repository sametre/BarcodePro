namespace BarcodePrinter.Themes;

public sealed record AppTheme(Color Background, Color Surface, Color Foreground, Color Muted, Color Border, Color Accent, Color Selection);
public static class ThemeManager
{
    public static bool IsDark { get; private set; }
    public static AppTheme Current => IsDark
        ? new(Color.FromArgb(35,38,41), Color.FromArgb(49,54,59), Color.FromArgb(238,238,236), Color.FromArgb(170,174,178), Color.FromArgb(78,83,88), Color.FromArgb(52,101,164), Color.FromArgb(62,86,112))
        : new(Color.FromArgb(230,230,228), Color.FromArgb(248,248,247), Color.FromArgb(42,44,46), Color.FromArgb(94,98,102), Color.FromArgb(184,186,184), Color.FromArgb(52,101,164), Color.FromArgb(203,220,239));
    public static void Toggle() { IsDark = !IsDark; foreach (Form form in Application.OpenForms) Apply(form); }
    public static void Apply(Control root)
    {
        var t = Current;
        root.BackColor = root is Form || root.Tag as string == "canvas" ? t.Background : t.Surface; root.ForeColor = root.Tag as string == "accent" ? t.Accent : t.Foreground;
        if (root is BarcodePrinter.Controls.Common.AppButton appButton) { appButton.BackColor=Color.FromArgb(45,48,51);appButton.ForeColor=Color.White;appButton.FlatAppearance.BorderColor=Color.FromArgb(31,33,35);appButton.FlatAppearance.BorderSize=1; }
        else if (root is Button b) { b.BackColor = b.Tag as string == "primary" ? t.Accent : t.Surface; b.ForeColor = b.Tag as string == "primary" ? Color.White : t.Foreground; }
        bool caption=false;for(Control? parent=root;parent!=null;parent=parent.Parent)if(parent.Tag as string=="caption"){caption=true;break;}
        if(caption){root.BackColor=Color.FromArgb(48,52,56);root.ForeColor=Color.White;}
        if (root is DataGridView g) { g.BackgroundColor = t.Surface; g.GridColor = t.Border; g.DefaultCellStyle.BackColor = t.Surface; g.DefaultCellStyle.ForeColor = t.Foreground; g.DefaultCellStyle.SelectionBackColor = t.Selection; g.DefaultCellStyle.SelectionForeColor = t.Foreground; g.ColumnHeadersDefaultCellStyle.BackColor = t.Background; g.ColumnHeadersDefaultCellStyle.ForeColor = t.Muted; g.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.Background; g.ColumnHeadersDefaultCellStyle.SelectionForeColor = t.Muted; }
        if (root is PropertyGrid p) { p.ViewBackColor = t.Surface; p.ViewForeColor = t.Foreground; p.HelpBackColor = t.Background; p.HelpForeColor = t.Foreground; p.LineColor = t.Border; }
        foreach (Control c in root.Controls) Apply(c);
        if (root is BarcodePrinter.Controls.Common.AppRibbon ribbon) ribbon.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppModuleMenu modules) modules.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppWorkspaceTabs tabs) tabs.RefreshTheme();
        root.Invalidate();
    }
}



