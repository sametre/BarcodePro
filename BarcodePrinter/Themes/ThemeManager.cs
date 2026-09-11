namespace BarcodePrinter.Themes;

public sealed record AppTheme(Color Background, Color Surface, Color Foreground, Color Muted, Color Border, Color Accent, Color Selection, Color ButtonGray, Color ButtonHover, Color ButtonActive, Color GradientStart, Color GradientEnd);
public static class ThemeManager
{
    public static bool IsDark { get; private set; } = false;
    public static AppTheme Current => IsDark
        ? new(Color.FromArgb(30,33,38),Color.FromArgb(39,43,49),Color.FromArgb(235,237,240),Color.FromArgb(164,172,183),Color.FromArgb(65,72,82),Color.FromArgb(64,136,212),Color.FromArgb(52,80,110),Color.FromArgb(28,32,38),Color.FromArgb(54,63,75),Color.FromArgb(21,27,34),Color.FromArgb(40,76,116),Color.FromArgb(36,64,97))
        : new(Color.FromArgb(232,234,237),Color.White,Color.FromArgb(42,46,51),Color.FromArgb(100,106,114),Color.FromArgb(190,195,201),Color.FromArgb(82,89,97),Color.FromArgb(218,222,227),Color.FromArgb(88,95,103),Color.FromArgb(108,115,123),Color.FromArgb(66,72,80),Color.FromArgb(106,112,120),Color.FromArgb(86,92,100));
    public static void Toggle() { IsDark=!IsDark;foreach(Form form in Application.OpenForms)Apply(form); }
    public static void Apply(Control root)
    {
        var t = Current;
        root.BackColor = root is Form || root.Tag as string == "canvas" ? t.Background : root.Tag as string == "section-heading" ? t.ButtonGray : t.Surface;
        root.ForeColor = root.Tag as string is "accent" ? t.Accent : root.Tag as string == "section-heading" ? Color.White : t.Foreground;

        if (root is BarcodePrinter.Controls.Common.AppButton appButton)
        {
            appButton.BackColor = appButton.Enabled?t.ButtonGray:t.Background;
            appButton.ForeColor = appButton.Enabled?Color.White:t.Muted;
            appButton.FlatAppearance.BorderColor = t.Border;
            appButton.FlatAppearance.BorderSize = 1;
            appButton.FlatAppearance.MouseOverBackColor = t.ButtonHover;
            appButton.FlatAppearance.MouseDownBackColor = t.ButtonActive;
            appButton.FlatStyle = FlatStyle.Flat;
        }
        else if (root is Button b)
        {
            // Modern Windows button styling
            if (b.Tag as string == "primary")
            {
                b.BackColor = t.Accent;
                b.ForeColor = Color.White;
            }
            else
            {
                b.BackColor = t.Surface;
                b.ForeColor = t.Foreground;
            }
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(219, 226, 232);
        }

        bool caption=false;
        for(Control? parent=root;parent!=null;parent=parent.Parent)
            if(parent.Tag as string=="caption"){caption=true;break;}
        if(caption)
        {
            root.BackColor=IsDark?Color.FromArgb(27,32,39):Color.FromArgb(218,225,233);
            root.ForeColor=t.Foreground;
        }

        if (root is DataGridView g)
        {
            g.BackgroundColor = t.Surface;
            g.GridColor = IsDark?Color.FromArgb(56,63,72):Color.FromArgb(224,230,237);
            g.DefaultCellStyle.BackColor = t.Surface;
            g.AlternatingRowsDefaultCellStyle.BackColor=IsDark?Color.FromArgb(34,39,46):Color.FromArgb(246,248,250);
            g.DefaultCellStyle.ForeColor = t.Foreground;
            g.DefaultCellStyle.SelectionBackColor = t.Selection;
            g.DefaultCellStyle.SelectionForeColor = t.Foreground;
            g.ColumnHeadersDefaultCellStyle.BackColor = t.Background;
            g.ColumnHeadersDefaultCellStyle.ForeColor = t.Foreground;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = t.Background;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = t.Foreground;
        }
        if (root is PropertyGrid p)
        {
            p.ViewBackColor = t.Surface;
            p.ViewForeColor = t.Foreground;
            p.HelpBackColor = t.Background;
            p.HelpForeColor = t.Foreground;
            p.LineColor = t.Border;
        }
        foreach (Control c in root.Controls) Apply(c);
        if (root is BarcodePrinter.Controls.Common.AppRibbon ribbon) ribbon.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppModuleMenu modules) modules.RefreshTheme();
        if (root is BarcodePrinter.Controls.Common.AppWorkspaceTabs tabs) tabs.RefreshTheme();
        root.Invalidate();
    }
}
