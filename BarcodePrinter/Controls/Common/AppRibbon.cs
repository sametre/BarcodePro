using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;
public sealed record RibbonCommand(string Text,AppIcon Icon,Action Execute);
public sealed class AppRibbon : UserControl
{
    private readonly FlowLayoutPanel tabs=new(){Dock=DockStyle.Top,Height=34,Padding=new Padding(12,0,0,0),WrapContents=false};
    private readonly Panel body=new(){Dock=DockStyle.Fill};
    private readonly Dictionary<string,FlowLayoutPanel> pages=[];
    private readonly Dictionary<string,Button> selectors=[];
    public string SelectedCategory {get;private set;}="";
    public AppRibbon(){Dock=DockStyle.Top;Height=124;Controls.Add(body);Controls.Add(tabs);}
    public void AddGroup(string category,string caption,params RibbonCommand[] commands)
    {
        if(!pages.TryGetValue(category,out var page))
        {
            page=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Padding=new Padding(12,5,8,0)};pages[category]=page;body.Controls.Add(page);
            var button=new AppButton{Text=category,AutoSize=false,Width=140,Height=32,IconKind=null,Padding=new Padding(14,4,14,4),Margin=Padding.Empty};button.Click+=(_,_)=>SelectCategory(category);selectors[category]=button;tabs.Controls.Add(button);
        }
        const int cellWidth=168;
        int columns=Math.Max(1,(commands.Length+1)/2);
        var group=new Panel{Width=cellWidth*columns+16,Height=82,Margin=new Padding(0,0,8,0)};
        var items=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=columns,RowCount=2,Padding=new Padding(0,0,8,0),Margin=Padding.Empty};
        for(int col=0;col<columns;col++)items.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));
        items.RowStyles.Add(new RowStyle(SizeType.Percent,50));items.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        for(int i=0;i<commands.Length;i++)
        {
            var command=commands[i];var button=new AppButton{Text=command.Text,IconKind=command.Icon,AutoSize=false,Dock=DockStyle.Fill,Margin=new Padding(0,0,6,2),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
            button.Click+=(_,_)=>command.Execute();items.Controls.Add(button,i/2,i%2);
        }
        var label=new Label{Text=caption,Dock=DockStyle.Bottom,Height=20,TextAlign=ContentAlignment.MiddleCenter,Font=new Font("Segoe UI",8),Tag="muted"};
        group.Controls.Add(items);group.Controls.Add(label);
        group.Paint+=(_,e)=>{using var pen=new Pen(ThemeManager.Current.Border);e.Graphics.DrawLine(pen,group.Width-1,2,group.Width-1,group.Height-4);};page.Controls.Add(group);        SelectCategory(SelectedCategory.Length==0?category:SelectedCategory);
    }
    public void SelectCategory(string category){if(!pages.ContainsKey(category))return;SelectedCategory=category;foreach(var p in pages)p.Value.Visible=p.Key==category;pages[category].BringToFront();RefreshTheme();}
    public void RefreshTheme(){foreach(var p in selectors){p.Value.BackColor=p.Key==SelectedCategory?ThemeManager.Current.Selection:ThemeManager.Current.Surface;p.Value.ForeColor=p.Key==SelectedCategory?ThemeManager.Current.Accent:ThemeManager.Current.Foreground;}}
}




