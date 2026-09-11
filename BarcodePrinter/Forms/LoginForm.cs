using BarcodePrinter.Controls.Common;
using BarcodePrinter.Services.Templates;
using BarcodePrinter.Themes;
namespace BarcodePrinter;

public sealed class LoginForm : AppWindow
{
    public static bool Authenticate(string username,string password)=>username=="owner"&&password=="owner";
    public LoginForm(CompanyProfile? company=null)
    {
        company??=CompanyProfile.Load();Text="R3 M-Kobi · Kullanıcı girişi";ClientSize=new Size(360,380);FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Padding=new Padding(32,18,32,22),BackColor=ThemeManager.Current.Surface};
        foreach(int height in new[]{58,30,22,30,22,30,48,24})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
        var brand=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};
        brand.Image=BrandAssets.CreateMark(54);
        var title=new Label{Text="R3 M-Kobi",Font=new Font("Segoe UI",16,FontStyle.Bold),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true,ForeColor=ThemeManager.Current.Foreground};
        var user=new AppTextBox{Dock=DockStyle.Fill,PlaceholderText="Kullanıcı kodu",AccessibleName="Kullanıcı kodu"};var password=new AppTextBox{Dock=DockStyle.Fill,UseSystemPasswordChar=true,AccessibleName="Şifre"};
        var subtitle=new Label{Text="Stok, ürün ve etiket yönetim paneli",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=ThemeManager.Current.Muted,Font=AppTypography.Body()};
        var bottom=new Panel{Dock=DockStyle.Fill};var error=new Label{Dock=DockStyle.Bottom,Height=22,ForeColor=Color.Firebrick,TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true};
        var login=new AppButton{Text="Giriş yap",IconKind=AppIcon.Products,Dock=DockStyle.Top,Tag="primary",Height=34,Font=new Font("Segoe UI",9,FontStyle.Bold)};
        login.Click+=(_,_)=>{if(Authenticate(user.Text,password.Text)){DialogResult=DialogResult.OK;Close();}else{error.Text="Kullanıcı kodu veya şifre hatalı.";password.Clear();password.Focus();}};
        bottom.Controls.Add(login);bottom.Controls.Add(error);layout.Controls.Add(brand,0,0);layout.Controls.Add(title,0,1);layout.Controls.Add(subtitle,0,2);layout.Controls.Add(new Label{Text="Kullanıcı kodu",Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft,Font=new Font("Segoe UI",9,FontStyle.Bold)},0,3);layout.Controls.Add(user,0,4);layout.Controls.Add(new Label{Text="Şifre",Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft,Font=new Font("Segoe UI",9,FontStyle.Bold)},0,5);layout.Controls.Add(password,0,6);layout.Controls.Add(bottom,0,7);Controls.Add(layout);
        AcceptButton=login;Shown+=(_,_)=>{ThemeManager.Apply(this);user.Focus();};Disposed+=(_,_)=>brand.Image?.Dispose();
    }
}
