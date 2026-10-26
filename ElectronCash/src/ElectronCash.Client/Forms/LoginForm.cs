using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Форма входа в систему (480x600).
/// </summary>
public class LoginForm : ModernForm
{
    private readonly InputField _loginField;
    private readonly InputField _passwordField;
    private readonly FlatButton _loginButton;
    private readonly Label _errorLabel;
    private readonly FlatButton _btnCashier;
    private readonly FlatButton _btnManager;
    private readonly FlatButton _btnAdmin;
    private readonly LinkLabel _registerLink;
    private bool _isLoggingIn;

    public LoginForm()
    {
        Text = "Электронная касса";
        Size = new Size(480, 600);
        MinimumSize = new Size(480, 600);
        MaximumSize = new Size(480, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(0, 40, 0, 0);

        // Поле логина
        _loginField = new InputField("Логин", "\uE716") // person
        {
            Location = new Point(40, 200),
            Size = new Size(400, 48),
            TabIndex = 0
        };

        // Поле пароля
        _passwordField = new InputField("Пароль", "\uE72E", true) // lock
        {
            Location = new Point(40, 268),
            Size = new Size(400, 48),
            TabIndex = 1
        };

        // Кнопка входа
        _loginButton = new FlatButton
        {
            Text = "Войти",
            Variant = ButtonVariant.Primary,
            Location = new Point(40, 336),
            Size = new Size(400, 48),
            Font = Theme.SectionTitleFont,
            TabIndex = 2
        };
        _loginButton.Click += LoginButton_Click;

        // Метка ошибки
        _errorLabel = new Label
        {
            Text = string.Empty,
            ForeColor = Theme.Danger,
            Font = Theme.SmallFont,
            AutoSize = false,
            Location = new Point(40, 392),
            Size = new Size(400, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };

        // Разделитель
        var separatorPanel = new Panel
        {
            Location = new Point(40, 432),
            Size = new Size(400, 1),
            BackColor = Theme.Border
        };

        var separatorLabel = new Label
        {
            Text = "или",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            BackColor = Theme.Background,
            Location = new Point(220, 424)
        };

        // Кнопки быстрого входа
        _btnCashier = CreateQuickLoginButton("Войти как кассир", "\uE716", 0);
        _btnManager = CreateQuickLoginButton("Войти как менеджер", "\uE710", 56);
        _btnAdmin = CreateQuickLoginButton("Войти как админ", "\uE72C", 112);

        // Ссылка на регистрацию
        _registerLink = new LinkLabel
        {
            Text = "Зарегистрироваться",
            Font = Theme.RegularFont,
            LinkColor = Theme.Accent,
            ActiveLinkColor = Theme.AccentHover,
            VisitedLinkColor = Theme.Accent,
            AutoSize = true,
            Location = new Point(180, 540),
            TabIndex = 6
        };
        _registerLink.LinkClicked += (s, e) => 
        {
            Toast.Show(this, "Регистрация недоступна в демо-версии", ToastType.Info);
        };

        Controls.AddRange(new Control[]
        {
            _loginField, _passwordField, _loginButton, _errorLabel,
            separatorPanel, separatorLabel,
            _btnCashier, _btnManager, _btnAdmin,
            _registerLink
        });

        // Enter в полях = вход
        _loginField.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _passwordField.Focus(); };
        _passwordField.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoginButton_Click(s, e); };
    }

    private FlatButton CreateQuickLoginButton(string text, string icon, int yOffset)
    {
        var btn = new FlatButton
        {
            Text = text,
            Icon = icon,
            Variant = ButtonVariant.Ghost,
            Location = new Point(40, 456 + yOffset),
            Size = new Size(400, 44),
            Font = Theme.RegularFont
        };
        btn.Click += QuickLogin_Click;
        return btn;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Логотип: круг 80x80 с градиентом Accent→Info
        var logoRect = new Rectangle(Width / 2 - 40, 60, 80, 80);
        using (var path = Theme.RoundedRect(logoRect, 40))
        using (var brush = Theme.CreateVerticalGradient(logoRect, Theme.Accent, Theme.Info))
        {
            g.FillPath(brush, path);
        }

        // Текст "ЭК" в логотипе
        using (var font = new Font(Theme.FontFamily, 28, FontStyle.Bold))
        using (var brush = new SolidBrush(Theme.Background))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("ЭК", font, brush, logoRect, sf);
        }

        // Заголовок
        var titleRect = new Rectangle(40, 156, Width - 80, 32);
        Theme.DrawText(g, "Электронная касса", Theme.FormTitleFont, Theme.TextPrimary, titleRect, StringAlignment.Center);

        // Подзаголовок
        var subtitleRect = new Rectangle(40, 184, Width - 80, 20);
        Theme.DrawText(g, "Автоматизированное рабочее место кассира", Theme.SmallFont, Theme.TextSecondary, subtitleRect, StringAlignment.Center);
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        if (_isLoggingIn) return;

        string login = _loginField.Text.Trim();
        string password = _passwordField.Text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ShowError("Введите логин и пароль");
            return;
        }

        _isLoggingIn = true;
        _loginButton.Loading = true;
        _errorLabel.Visible = false;

        try
        {
            // Имитация задержки сети
            await Task.Delay(1000);

            var user = MockDataService.Instance.GetUserByCredentials(login, password);
            if (user != null)
            {
                Session.Login(user);
                Toast.Show(this, $"Добро пожаловать, {user.FullName}!", ToastType.Success);
                
                await Task.Delay(500);
                
                // Открываем главную форму
                var mainForm = new MainForm();
                mainForm.Show();
                Hide();
            }
            else
            {
                ShowError("Неверный логин или пароль");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            _isLoggingIn = false;
            _loginButton.Loading = false;
        }
    }

    private void QuickLogin_Click(object? sender, EventArgs e)
    {
        if (sender == _btnCashier)
        {
            _loginField.Text = "cashier";
            _passwordField.Text = "cashier";
        }
        else if (sender == _btnManager)
        {
            _loginField.Text = "manager";
            _passwordField.Text = "manager";
        }
        else if (sender == _btnAdmin)
        {
            _loginField.Text = "admin";
            _passwordField.Text = "admin";
        }
        
        LoginButton_Click(sender, e);
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
        _loginField.HasError = true;
        _passwordField.HasError = true;
    }
}