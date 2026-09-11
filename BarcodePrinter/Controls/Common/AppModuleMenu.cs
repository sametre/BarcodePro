using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;

public sealed class AppModuleMenu : ToolStrip
{
    private readonly List<Image> icons=[];
    public AppModuleMenu()
    {
        Dock=DockStyle.Top;GripStyle=ToolStripGripStyle.Hidden;AutoSize=false;Height=48;Padding=new Padding(6,2,6,2);ImageScalingSize=new Size(20,20);Font=AppTypography.Body();Renderer=new AppMenuRenderer();
    }
    public void AddModule(string title,AppIcon icon,params RibbonCommand[] commands)
    {
        var image=AppIcons.Create(icon,ThemeManager.Current.Accent,20);icons.Add(image);
        var item=new ToolStripDropDownButton(title,image){AutoSize=false,Size=new Size(78,43),TextImageRelation=TextImageRelation.ImageAboveText,ImageScaling=ToolStripItemImageScaling.None,ShowDropDownArrow=true,ToolTipText=title+" işlemleri"};
        foreach(var command in commands)
        {
            var small=AppIcons.Create(command.Icon,ThemeManager.Current.Accent,16);icons.Add(small);var child=new ToolStripMenuItem(command.Text,small,(_,_)=>command.Execute()){Padding=new Padding(4,3,10,3)};item.DropDownItems.Add(child);
        }
        Items.Add(item);
    }
    public void AddAction(string title,AppIcon icon,Action action)
    {
        var image=AppIcons.Create(icon,ThemeManager.Current.ButtonGray,20);icons.Add(image);Items.Add(new ToolStripButton(title,image,(_,_)=>action()){AutoSize=false,Size=new Size(62,43),TextImageRelation=TextImageRelation.ImageAboveText,ImageScaling=ToolStripItemImageScaling.None});
    }
    public void AddSearch(Action<string> search)
    {
        var field=new ToolStripTextBox{AutoSize=false,Width=195,Alignment=ToolStripItemAlignment.Right,ToolTipText="Ürün veya barkod ara; Enter ile açın",Margin=new Padding(10,10,6,8)};field.TextBox.PlaceholderText="Ürün / barkod ara…";field.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;search(field.Text);}};Items.Add(field);
    }
    public void RefreshTheme()
    {
        BackColor=ThemeManager.Current.Surface;ForeColor=ThemeManager.Current.Foreground;
        foreach(ToolStripItem item in Items){item.ForeColor=ForeColor;if(item is ToolStripDropDownItem parent){parent.DropDown.BackColor=BackColor;parent.DropDown.ForeColor=ForeColor;foreach(ToolStripItem child in parent.DropDownItems){child.BackColor=BackColor;child.ForeColor=ForeColor;}}}
    }
    protected override void Dispose(bool disposing){if(disposing)foreach(var image in icons)image.Dispose();base.Dispose(disposing);}
}
public sealed class AppWorkspaceTabs : ToolStrip
{
    private readonly Dictionary<string,ToolStripButton> pages=[];
    public event Action<string>? PageSelected;
    public string ActivePage{get;private set;}="";
    public AppWorkspaceTabs(){Renderer=new AppMenuRenderer();Dock=DockStyle.Top;GripStyle=ToolStripGripStyle.Hidden;AutoSize=false;Height=28;Padding=new Padding(6,2,6,1);Font=AppTypography.Body();}
    public void ActivatePage(string title)
    {
        if(!pages.TryGetValue(title,out var tab)){tab=new ToolStripButton(title){AutoSize=false,Width=Math.Max(88,TextRenderer.MeasureText(title,Font).Width+22),Height=24,Margin=new Padding(0,0,3,0)};tab.Click+=(_,_)=>PageSelected?.Invoke(title);pages.Add(title,tab);Items.Add(tab);}
        ActivePage=title;RefreshTheme();
    }
    public void RefreshTheme(){BackColor=ThemeManager.Current.Accent;foreach(var p in pages){p.Value.BackColor=p.Key==ActivePage?ThemeManager.Current.Surface:ThemeManager.Current.Accent;p.Value.ForeColor=p.Key==ActivePage?ThemeManager.Current.Foreground:Color.White;}}
}

