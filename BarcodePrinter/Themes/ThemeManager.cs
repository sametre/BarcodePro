namespace BarcodePrinter.Themes;

public sealed record AppTheme(Color Background, Color Surface, Color Foreground, Color Muted, Color Border, Color Accent, Color Selection);
public static class ThemeManager
{
    public static bool IsDark { get; private set; }
    public static AppTheme Current => IsDark
        ? new(Color.FromArgb(15,17,20), Color.FromArgb(27,30,35), Color.FromArgb(239,241,244), Color.FromArgb(158,165,175), Color.FromArgb(57,62,70), Color.FromArgb(10,12,15), Color.FromArgb(48,52,59))
        : new(Color.FromArgb(243,244,246), Color.White, Color.FromArgb(26,29,34), Color.FromArgb(101,107,116), Color.FromArgb(211,214,219), Color.FromArgb(25,27,31), Color.FromArgb(226,228,232));
    public static void Toggle() { IsDark = !IsDark; foreach (Form form in Application.OpenForms) Apply(form); }
    public static void Apply(Control root)
    {
        var t = Current;
        root.BackColor = root is Form || root.Tag as string == "canvas" ? t.Background : t.Surface; root.ForeColor = root.Tag as string == "accent" ? t.Accent : t.Foreground;
        if (root is BarcodePrinter.Controls.Common.AppButton appButton) { appButton.BackColor=Color.FromArgb(25,27,31);appButton.ForeColor=Color.White;appButton.FlatAppearance.BorderColor=Color.FromArgb(25,27,31); }
        else if (root is Button b) { b.BackColor = b.Tag as string == "primary" ? t.Accent : t.Surface; b.ForeColor = b.Tag as string == "primary" ? Color.White : t.Foreground; }
        bool caption=false;for(Control? parent=root;parent!=null;parent=parent.Parent)if(parent.Tag as string=="caption"){caption=true;break;}
        if(caption){root.BackColor=Color.FromArgb(20,22,26);root.ForeColor=Color.White;}
        if (root is DataGridView g) { g.BackgroundColor = t.Surface; g.GridColor = t.Border; g.DefaultCellStyle.BackColor = t.Surface; g.DefaultCellStyle.ForeColor = t.Foreground; g.DefaultCellStyle.SelectionBackColor = t.Selection; g.DefaultCellStyle.SelectionForeColor = t.Foreground; g.ColumnHeadersDefaultCellStyle.BackColor = t.Background; g.ColumnHeadersDefaultCellStyle.ForeColor = t.Muted; g.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.Background; g.ColumnHeadersDefaultCellStyle.SelectionForeColor = t.Muted; }
        if (root is PropertyGrid p) { p.ViewBackColor = t.Surface; p.ViewForeColor = t.Foreground; p.HelpBackColor = t.Background; p.HelpForeColor = t.Foreground; p.LineColor = t.Border; }
        foreach (Control c in root.Controls) Apply(c);
        if (root is BarcodePrinter.Controls.Common.AppRibbon ribbon) ribbon.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppModuleMenu modules) modules.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppWorkspaceTabs tabs) tabs.RefreshTheme();
        root.Invalidate();
    }
}



