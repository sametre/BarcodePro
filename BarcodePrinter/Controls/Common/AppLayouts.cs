using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;

/// <summary>Shared two-column field layout: labels and editors share row baselines.</summary>
public sealed class AppFieldGrid : TableLayoutPanel
{
    public AppFieldGrid(int labelWidth=150)
    {
        Dock=DockStyle.Top;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;ColumnCount=2;Margin=Padding.Empty;Padding=new Padding(0);
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,labelWidth));ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
    }
    public Label AddField(string caption,Control editor,int rowHeight=34)
    {
        int row=RowCount++;RowStyles.Add(new RowStyle(SizeType.Absolute,rowHeight));
        var label=new Label{Text=caption,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0,0,12,0),AutoEllipsis=true};
        editor.Margin=new Padding(0,4,0,4);editor.Dock=DockStyle.Fill;
        Controls.Add(label,0,row);Controls.Add(editor,1,row);return label;
    }
}
public sealed class AppSection : Panel
{
    public Panel Body { get; }=new(){Dock=DockStyle.Fill,Padding=new Padding(8)};
    public AppSection(string title)
    {
        Dock=DockStyle.Fill;Margin=new Padding(0,0,8,0);Padding=new Padding(1);
        var heading=new Label{Text=title,Dock=DockStyle.Top,Height=28,Padding=new Padding(8,6,0,0),Font=new Font("Segoe UI",9,FontStyle.Bold)};
        Controls.Add(Body);Controls.Add(heading);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(ThemeManager.Current.Border);e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);}
}
