using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;

public class AppButton : Button
{
    private AppIcon? iconKind;
    public AppIcon? IconKind { get=>iconKind; set { iconKind=value;RefreshIcon(); } }
    public AppButton()
    {
        FlatStyle=FlatStyle.Flat;
        FlatAppearance.BorderSize=1;
        FlatAppearance.BorderColor=Color.FromArgb(200, 220, 240);
        Cursor=Cursors.Hand;
        AutoSize=true;
        Height=26;
        MinimumSize=new Size(0,26);
        Padding=new Padding(6,2,6,2);
        Margin=new Padding(0,0,5,0);
        Font=AppTypography.Body();
        ImageAlign=ContentAlignment.MiddleLeft;
        TextImageRelation=TextImageRelation.ImageBeforeText;
        TextAlign=ContentAlignment.MiddleCenter;
        BackColor=ThemeManager.Current.ButtonGray;
        ForeColor=Color.White;
    }
    public override Size GetPreferredSize(Size proposedSize)
    {
        var size=base.GetPreferredSize(proposedSize);
        return new Size(size.Width,Math.Max(26,(int)(26*DeviceDpi/96f)));
    }
    protected override void OnForeColorChanged(EventArgs e){base.OnForeColorChanged(e);RefreshIcon();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);ThemeManager.Apply(this);}
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);RefreshIcon();}
    private void RefreshIcon(){var old=Image;Image=iconKind.HasValue?AppIcons.Create(iconKind.Value,ForeColor,Math.Max(16,16*DeviceDpi/96)):null;old?.Dispose();}
    protected override void Dispose(bool disposing){if(disposing){var old=Image;Image=null;old?.Dispose();}base.Dispose(disposing);}
}
public class AppTextBox : TextBox { public AppTextBox() { BorderStyle = BorderStyle.FixedSingle; Font = AppTypography.Body(); } }
public class AppComboBox : ComboBox { public AppComboBox() { FlatStyle = FlatStyle.Flat; DropDownStyle = ComboBoxStyle.DropDownList; } }
public class AppNumericInput : NumericUpDown { public AppNumericInput() { DecimalPlaces = 2; Maximum = 1000000000; BorderStyle = BorderStyle.FixedSingle; } }
public class AppDatePicker : DateTimePicker { public AppDatePicker() { Format = DateTimePickerFormat.Short; } }
public class AppToggle : CheckBox { public AppToggle() { AutoSize = true; FlatStyle = FlatStyle.Flat; } }
public class AppBadge : Label { public AppBadge() { AutoSize = true; Padding = new Padding(8,4,8,4); } }
public class AppPanel : Panel { public AppPanel() { DoubleBuffered = true; Padding = new Padding(10); } }
public class AppCard : AppPanel { protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using var pen = new Pen(ThemeManager.Current.Border); e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1); } }
public class AppDialog : AppWindow { public AppDialog() { StartPosition = FormStartPosition.CenterParent; Font = AppTypography.Body(); MinimumSize = new Size(640,480); } protected override void OnShown(EventArgs e) { base.OnShown(e); var area = Screen.FromControl(this).WorkingArea; Size = new Size(Math.Min(Width,area.Width),Math.Min(Height,area.Height)); ThemeManager.Apply(this); } }
public class AppDataGrid : DataGridView
{
    public AppDataGrid()
    {
        DoubleBuffered = true; Dock = DockStyle.Fill; BorderStyle = BorderStyle.FixedSingle; AllowUserToAddRows = false; AllowUserToDeleteRows = false; ReadOnly = true; MultiSelect = true; SelectionMode = DataGridViewSelectionMode.FullRowSelect; AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; RowHeadersVisible = false; EnableHeadersVisualStyles = false; ColumnHeadersBorderStyle=DataGridViewHeaderBorderStyle.Single; CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal; RowTemplate.Height = 27; ColumnHeadersHeight = 28; ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing; AllowUserToOrderColumns = true;
        var menu=new AppContextMenu();menu.Items.Add("Hücreyi kopyala",null,(_,_)=>{if(CurrentCell?.Value is object value)Clipboard.SetText(value.ToString()??"");});menu.Items.Add("Satırı kopyala",null,(_,_)=>{if(CurrentRow!=null)Clipboard.SetText(string.Join("\t",CurrentRow.Cells.Cast<DataGridViewCell>().Where(c=>c.Visible).Select(c=>c.Value?.ToString()??"")));});ContextMenuStrip=menu;Disposed+=(_,_)=>menu.Dispose();
        CellMouseDown+=(_,e)=>{if(e.Button==MouseButtons.Right&&e.RowIndex>=0&&!Rows[e.RowIndex].Selected){ClearSelection();Rows[e.RowIndex].Selected=true;if(e.ColumnIndex>=0)CurrentCell=Rows[e.RowIndex].Cells[e.ColumnIndex];}};
        DefaultCellStyle.Font = AppTypography.Body(); ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI",9,FontStyle.Bold); DefaultCellStyle.Padding = new Padding(5,2,5,2); ThemeManager.Apply(this);
    }
}
public class AppSidebar : FlowLayoutPanel
{
    public bool Collapsed { get; private set; }
    public AppSidebar() { Dock = DockStyle.Left; Width = 228; FlowDirection = FlowDirection.TopDown; WrapContents = false; AutoScroll = true; Padding = new Padding(12,20,8,12); }
    public void Toggle() { Collapsed = !Collapsed; Width = Collapsed ? 68 : 228; foreach (Control c in Controls) if (c is Button b && b.AccessibleName is string title) { b.Text = Collapsed ? title[..1] : title; b.Width = Collapsed ? 40 : 198; } }
}
public class AppToolbar : FlowLayoutPanel
{
    private bool arranging;
    public AppToolbar(){Dock=DockStyle.Top;AutoSize=true;MinimumSize=new Size(0,36);Padding=new Padding(6,4,6,4);WrapContents=true;}
    protected override void OnLayout(LayoutEventArgs levent)
    {
        if(!arranging)
        {
            arranging=true;
            try
            {
                int rowHeight=Math.Max(26,26*DeviceDpi/96);
                foreach(Control c in Controls)
                {
                    if(!c.Visible)continue;
                    var height=c.AutoSize?c.GetPreferredSize(Size.Empty).Height:c.Height;
                    int top=Math.Max(0,(rowHeight-height)/2);
                    var margin=new Padding(0,top,6,Math.Max(2,rowHeight-height-top+2));
                    if(c.Margin!=margin)c.Margin=margin;
                }
            } finally {arranging=false;}
        }
        base.OnLayout(levent);
    }
}public sealed class AppToast : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 3500 };
    public AppToast(string message) { FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; Size = new Size(420,80); Controls.Add(new Label { Text = message, Dock = DockStyle.Fill, Padding = new Padding(16), TextAlign = ContentAlignment.MiddleLeft }); timer.Tick += (_,_) => Close(); Shown += (_,_) => { ThemeManager.Apply(this); timer.Start(); }; }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
}
