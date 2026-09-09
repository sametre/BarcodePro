using System.Runtime.InteropServices;
using BarcodePrinter.Themes;
namespace BarcodePrinter.Controls.Common;

/// <summary>Compact branded caption while retaining the native resizable window frame.</summary>
public class AppWindow : Form
{
    private Panel? caption;private Label? title;private Button? maximize;
    private readonly Icon brandIcon;
    private Rectangle normalBounds;private FormWindowState previousState;private bool restoring;
    public AppWindow()
    {
        SetStyle(ControlStyles.ResizeRedraw,true);Padding=new Padding(1);Font=AppTypography.Body();using var stream=new MemoryStream(BrandAssets.CreateIcon());using var icon=new Icon(stream);brandIcon=(Icon)icon.Clone();Icon=brandIcon;
    }
    protected override CreateParams CreateParams {get{var cp=base.CreateParams;cp.Style&=~0x00C00000;return cp;}}
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);if(caption!=null)return;
        caption=new Panel{Dock=DockStyle.Top,Height=38,Padding=new Padding(10,0,0,0),Tag="caption"};
        var logo=new PictureBox{Dock=DockStyle.Left,Width=30,Image=BrandAssets.CreateMark(28),SizeMode=PictureBoxSizeMode.CenterImage};
        title=new Label{Text=Text,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(8,0,0,0),Font=new Font("Segoe UI",9.5f,FontStyle.Bold)};
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Right,Width=132,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=Padding.Empty,Padding=Padding.Empty};
        Button CaptionButton(string text,Action action){var b=new Button{Text=text,Width=44,Height=38,FlatStyle=FlatStyle.Flat,Margin=Padding.Empty,TabStop=false,Font=new Font("Segoe UI",11),AccessibleName=text};b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>action();buttons.Controls.Add(b);return b;}
        var minimize=CaptionButton("−",()=>WindowState=FormWindowState.Minimized);minimize.AccessibleName="Simge durumuna küçült";minimize.Enabled=MinimizeBox;
        maximize=CaptionButton("□",()=>ToggleMaximize());maximize.AccessibleName="Büyüt / geri yükle";maximize.Enabled=MaximizeBox;
        var close=CaptionButton("×",Close);close.AccessibleName="Kapat";close.FlatAppearance.MouseOverBackColor=Color.FromArgb(218,66,70);
        caption.Controls.Add(title);caption.Controls.Add(logo);caption.Controls.Add(buttons);Controls.Add(caption);caption.SendToBack();
        foreach(var surface in new Control[]{caption,title,logo}){surface.MouseDown+=Drag;surface.DoubleClick+=(_,_)=>ToggleMaximize();}
        ThemeManager.Apply(caption);
    }
    private void ToggleMaximize(){if(MaximizeBox)WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
    private void Drag(object? sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0x00A1,(IntPtr)2,IntPtr.Zero);}}
    protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);if(title!=null)title.Text=Text;}
    protected override void OnResize(EventArgs e)
    {
        var prior=previousState;previousState=WindowState;base.OnResize(e);
        if(maximize!=null)maximize.Text=WindowState==FormWindowState.Maximized?"❐":"□";
        if(restoring)return;
        if(WindowState==FormWindowState.Normal)
        {
            // WinForms otherwise adds the hidden native frame to the restored client size.
            if(prior!=FormWindowState.Normal&&!normalBounds.IsEmpty){var target=normalBounds;BeginInvoke(()=>{if(IsDisposed||WindowState!=FormWindowState.Normal)return;restoring=true;try{Bounds=target;normalBounds=target;}finally{restoring=false;}});}
            normalBounds=Bounds;
        }
    }
    protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);if(!restoring&&WindowState==FormWindowState.Normal&&previousState==FormWindowState.Normal)normalBounds=Bounds;}
    protected override void OnLayout(LayoutEventArgs e){base.OnLayout(e);if(caption!=null)caption.SetBounds(1,1,ClientSize.Width-2,caption.Height);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(ThemeManager.Current.Accent);e.Graphics.DrawRectangle(pen,0,0,ClientSize.Width-1,ClientSize.Height-1);}
    protected override void WndProc(ref Message m)
    {
        // Extend the client area through the old grey frame; keep native sizing styles.
        if(m.Msg==0x0083){m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x0024)
        {
            base.WndProc(ref m);var info=Marshal.PtrToStructure<MinMaxInfo>(m.LParam);var screen=Screen.FromHandle(Handle);var work=screen.WorkingArea;var bounds=screen.Bounds;
            info.MaxPosition=new Point(work.Left-bounds.Left,work.Top-bounds.Top);info.MaxSize=work.Size;Marshal.StructureToPtr(info,m.LParam,false);return;
        }
        base.WndProc(ref m);
        if(m.Msg==0x0084&&WindowState==FormWindowState.Normal&&FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow)
        {
            long position=m.LParam.ToInt64();var point=PointToClient(new Point(unchecked((short)(position&0xffff)),unchecked((short)((position>>16)&0xffff))));int edge=Math.Max(5,DeviceDpi*5/96);
            bool left=point.X<edge,right=point.X>=ClientSize.Width-edge,top=point.Y<edge,bottom=point.Y>=ClientSize.Height-edge;
            int hit=top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:0;
            if(hit!=0)m.Result=(IntPtr)hit;
        }
    }
    [StructLayout(LayoutKind.Sequential)]private struct MinMaxInfo{public Point Reserved;public Size MaxSize;public Point MaxPosition;public Size MinTrackSize;public Size MaxTrackSize;}
    protected override void Dispose(bool disposing){if(disposing){if(caption!=null)foreach(var picture in caption.Controls.OfType<PictureBox>())picture.Image?.Dispose();brandIcon.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
}


