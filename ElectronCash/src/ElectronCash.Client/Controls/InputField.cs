using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Поле ввода с иконкой и placeholder (используется в LoginForm).
/// </summary>
public class InputField : Control
{
    private readonly TextBox _textBox;
    private readonly string _placeholder;
    private readonly string _icon;
    private bool _showPassword;
    private bool _focused;
    private bool _hasError;
    private string _errorText = string.Empty;

    public InputField(string placeholder, string icon, bool isPassword = false)
    {
        _placeholder = placeholder;
        _icon = icon;
        _showPassword = !isPassword;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Font = Theme.RegularFont;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            Location = new Point(48, 12),
            Width = 200,
            Height = 24,
            UseSystemPasswordChar = isPassword,
            TabStop = true
        };
        _textBox.TextChanged += (s, e) => { _hasError = false; Invalidate(); };
        _textBox.Enter += (s, e) => { _focused = true; Invalidate(); };
        _textBox.Leave += (s, e) => { _focused = false; Invalidate(); };
        _textBox.KeyDown += (s, e) => OnKeyDown(e);
        
        Controls.Add(_textBox);

        // Set size AFTER _textBox is created to avoid OnResize accessing null _textBox
        Height = 48;
        MinimumSize = new Size(200, 48);
    }

    public override string? Text 
    { 
        get => _textBox.Text; 
        set => _textBox.Text = value ?? string.Empty; 
    }

    public bool UseSystemPasswordChar
    {
        get => _textBox.UseSystemPasswordChar;
        set => _textBox.UseSystemPasswordChar = value;
    }

    public bool ShowPassword
    {
        get => _showPassword;
        set { _showPassword = value; _textBox.UseSystemPasswordChar = !value; Invalidate(); }
    }

    public bool HasError
    {
        get => _hasError;
        set { _hasError = value; Invalidate(); }
    }

    public string ErrorText
    {
        get => _errorText;
        set { _errorText = value; _hasError = !string.IsNullOrEmpty(value); Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Фон
        Color bgColor = _hasError ? Color.FromArgb(60, 30, 30) : (_focused ? Theme.SurfaceElevated : Theme.Surface);
        Color borderColor = _hasError ? Theme.Danger : (_focused ? Theme.Accent : Theme.Border);

        using (var path = Theme.RoundedRect(rect, 8))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }
            using (var pen = new Pen(borderColor, _focused || _hasError ? 2 : 1))
            {
                g.DrawPath(pen, path);
            }
        }

        // Иконка слева
        using (var iconFont = new Font(Theme.IconFontFamily, 16))
        using (var brush = new SolidBrush(_focused ? Theme.Accent : Theme.TextMuted))
        {
            var iconRect = new Rectangle(12, 8, 32, 32);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_icon, iconFont, brush, iconRect, sf);
        }

        // Placeholder (рисуем если поле пустое и не в фокусе)
        if (string.IsNullOrEmpty(_textBox.Text) && !_focused)
        {
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                var textRect = new Rectangle(48, 0, Width - 96, Height);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString(_placeholder, Theme.RegularFont, brush, textRect, sf);
            }
        }

        // Кнопка показать/скрыть пароль (если это пароль)
        if (_textBox.UseSystemPasswordChar != _showPassword) // is password field
        {
            string eyeIcon = _showPassword ? "\uE890" : "\uE891"; // eye / eye closed
            var eyeRect = new Rectangle(Width - 44, 8, 32, 32);
            
            using (var iconFont = new Font(Theme.IconFontFamily, 14))
            using (var brush = new SolidBrush(_focused ? Theme.Accent : Theme.TextMuted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(eyeIcon, iconFont, brush, eyeRect, sf);
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        
        // Клик по иконке глаза
        var eyeRect = new Rectangle(Width - 44, 8, 32, 32);
        if (_textBox.UseSystemPasswordChar != _showPassword && eyeRect.Contains(e.Location))
        {
            _showPassword = !_showPassword;
            _textBox.UseSystemPasswordChar = !_showPassword;
            _textBox.Focus();
            Invalidate();
        }
        else
        {
            _textBox.Focus();
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_textBox != null)
        {
            _textBox.Width = Width - 96;
        }
    }

    public void ApplyTheme()
    {
        _textBox.Font = Theme.RegularFont;
        _textBox.ForeColor = Theme.TextPrimary;
        Invalidate();
    }
}