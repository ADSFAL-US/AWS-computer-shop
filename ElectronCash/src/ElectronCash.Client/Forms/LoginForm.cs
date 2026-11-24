using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Форма входа. Контент центрируется и масштабируется вместе с окном.
/// </summary>
public class LoginForm : ModernForm
{
    private const int ContentMaxWidth = 400;
    private const int ContentMinWidth = 260;

    private readonly InputField _loginField;
    private readonly InputField _passwordField;
    private readonly FlatButton _loginButton;
    private readonly Label _errorLabel;
    private readonly FlatButton _btnAdmin;
    private readonly Label _registerLink;
    private readonly TableLayoutPanel _content;
    private bool _isLoggingIn;

    public LoginForm()
    {
        Text = "Электронная касса";
        Size = new Size(480, 720);
        MinimumSize = new Size(420, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(0, 40, 0, 0);

        // Внешняя сетка: шапка с логотипом, контент, ссылка снизу
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Padding = new Padding(24, 0, 24, 24)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 236f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));

        var header = new LoginHeaderPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        _content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // логин
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // пароль
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));   // кнопка входа
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));   // ошибка
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // разделитель
        _content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // быстрый вход

        _loginField = new InputField("Логин", "\uE716")
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            TabIndex = 0
        };

        _passwordField = new InputField("Пароль", "\uE72E", true)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            TabIndex = 1
        };

        _loginButton = new FlatButton
        {
            Text = "Войти",
            Variant = ButtonVariant.Primary,
            Dock = DockStyle.Fill,
            Font = Theme.SectionTitleFont,
            TabIndex = 2,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 8, 0, 8)
        };
        _loginButton.Click += LoginButton_Click;

        _errorLabel = new Label
        {
            Text = string.Empty,
            ForeColor = Theme.Danger,
            Font = Theme.SmallFont,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
            Margin = Padding.Empty
        };

        var separatorPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Theme.Border,
            Margin = new Padding(0, 20, 0, 0)
        };

        var separatorLabel = new Label
        {
            Text = "или",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            BackColor = Theme.Background,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty
        };

        var separatorHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        separatorHost.Controls.Add(separatorPanel);
        separatorHost.Controls.Add(separatorLabel);
        separatorHost.Resize += (s, e) =>
        {
            separatorPanel.Location = new Point(0, separatorHost.Height / 2);
            separatorPanel.Width = separatorHost.Width;
            separatorLabel.Location = new Point(
                (separatorHost.Width - separatorLabel.Width) / 2,
                (separatorHost.Height - separatorLabel.Height) / 2);
        };

        _btnAdmin = new FlatButton
        {
            Text = "Войти как админ",
            Icon = "\uE72C",
            Variant = ButtonVariant.Ghost,
            Dock = DockStyle.Top,
            Height = 44,
            Font = Theme.RegularFont,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 8, 0, 0)
        };
        _btnAdmin.Click += QuickLogin_Click;

        _content.Controls.Add(_loginField, 0, 0);
        _content.Controls.Add(_passwordField, 0, 1);
        _content.Controls.Add(_loginButton, 0, 2);
        _content.Controls.Add(_errorLabel, 0, 3);
        _content.Controls.Add(separatorHost, 0, 4);
        _content.Controls.Add(_btnAdmin, 0, 5);

        _registerLink = new Label
        {
            Text = "Нет аккаунта? Обратитесь к администратору",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty
        };

        var linkHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        linkHost.Controls.Add(_registerLink);
        linkHost.Resize += (s, e) => _registerLink.Location = new Point(
            (linkHost.Width - _registerLink.Width) / 2,
            (linkHost.Height - _registerLink.Height) / 2);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_content, 0, 1);
        root.Controls.Add(linkHost, 0, 2);
        Controls.Add(root);

        _loginField.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _passwordField.Focus(); };
        _passwordField.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoginButton_Click(s, e); };
    }

    /// <summary>Ограничивает ширину контента, чтобы на широком окне поля не растягивались.</summary>
    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        // OnLayout вызывается и из конструктора базовой формы, когда поля ещё не созданы
        if (_content is null) return;

        int available = ClientSize.Width - 48;
        int width = Math.Clamp(available, ContentMinWidth, ContentMaxWidth);
        int left = (ClientSize.Width - width) / 2;

        _content.Padding = new Padding(Math.Max(0, left - 24), 0, Math.Max(0, left - 24), 0);
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        if (_isLoggingIn) return;

        string login = _loginField.Text.Trim();
        string password = _passwordField.Text ?? string.Empty;

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
            var user = await ApiClient.Instance.LoginAsync(login, password);
            Session.Login(user);
            Toast.Show(this, $"Добро пожаловать, {user.FullName}!", ToastType.Success);

            await Task.Delay(500);

            var mainForm = new MainForm();
            mainForm.Show();
            Hide();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
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
        _loginField.Text = "admin";
        _passwordField.Text = "admin";

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

/// <summary>
/// Шапка формы входа: логотип, заголовок и подзаголовок, отрисованные по центру.
/// </summary>
internal sealed class LoginHeaderPanel : Panel
{
    public LoginHeaderPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int centerX = Width / 2;

        var logoRect = new Rectangle(centerX - 40, 24, 80, 80);
        using (var path = Theme.RoundedRect(logoRect, 40))
        using (var brush = Theme.CreateVerticalGradient(logoRect, Theme.Accent, Theme.Info))
        {
            g.FillPath(brush, path);
        }

        using (var font = new Font(Theme.FontFamily, 28, FontStyle.Bold))
        using (var brush = new SolidBrush(Theme.Background))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("ЭК", font, brush, logoRect, sf);
        }

        var titleRect = new Rectangle(0, 120, Width, 32);
        Theme.DrawText(g, "Электронная касса", Theme.FormTitleFont, Theme.TextPrimary, titleRect, StringAlignment.Center);

        var subtitleRect = new Rectangle(0, 160, Width, 24);
        Theme.DrawText(g, "Автоматизированное рабочее место кассира", Theme.SmallFont, Theme.TextSecondary, subtitleRect, StringAlignment.Center);
    }
}
