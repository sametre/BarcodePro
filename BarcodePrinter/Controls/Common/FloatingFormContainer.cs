using BarcodePrinter.Themes;

namespace BarcodePrinter.Controls.Common;

/// <summary>
/// Linux/XFCE tarzında kayan pencere konteyneri (draggable windows)
/// Formları içinde pencere olarak gösterir
/// </summary>
public sealed class FloatingFormContainer : Panel
{
    private Form? _currentForm;
    private Panel? _titleBar;
    private Label? _titleLabel;
    private Button? _closeButton;
    private Point _lastMousePos;
    private bool _isDragging;

    public FloatingFormContainer()
    {
        Dock = DockStyle.Fill;
        BackColor = ThemeManager.Current.Background;
    }

    public void ShowForm(Form form)
    {
        // Eski formu kapat
        if (_currentForm != null)
        {
            _currentForm.Dispose();
        }

        _currentForm = form;

        // Konteneyi temizle
        Controls.Clear();

        // Başlık çubuğu oluştur
        _titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(52, 152, 219),
            Padding = new Padding(12, 6, 8, 6)
        };

        _titleLabel = new Label
        {
            Text = form.Text,
            Font = AppTypography.Heading(),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _closeButton = new Button
        {
            Text = "✕",
            Width = 28,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Dock = DockStyle.Right,
            Margin = new Padding(4, 0, 0, 0)
        };

        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 76, 60);
        _closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 57, 43);
        _closeButton.Click += (_,_) => CloseForm();
        _closeButton.MouseEnter += (_,_) => _closeButton.BackColor = Color.FromArgb(231, 76, 60);
        _closeButton.MouseLeave += (_,_) => _closeButton.BackColor = Color.FromArgb(52, 152, 219);

        // Başlık çubuğu drag işlemi
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += TitleBar_MouseUp;
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += TitleBar_MouseUp;

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_closeButton);

        // İçerik paneli
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeManager.Current.Surface,
            Padding = new Padding(0)
        };

        // Form kontentini kontakt etme (Form kullanmıyoruz, UserControl benzeri taşıyoruz)
        // Bunun yerine form öğelerini taravsızlaştırıp konteneye ekliyoruz
        contentPanel.AutoScroll = true;

        Controls.Add(contentPanel);
        Controls.Add(_titleBar);

        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(form);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastMousePos = e.Location;
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _titleBar != null)
        {
            var delta = new Point(e.X - _lastMousePos.X, e.Y - _lastMousePos.Y);
            // Burada pencereyi şu anda kayan bir pencere olarak hareket ettirebilirsiniz
            // Şimdilik devre dışı bıraktık, gelecekte tab sistemi için
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    public void CloseForm()
    {
        if (_currentForm != null)
        {
            _currentForm.Dispose();
            _currentForm = null;
        }
        Controls.Clear();
    }
}
