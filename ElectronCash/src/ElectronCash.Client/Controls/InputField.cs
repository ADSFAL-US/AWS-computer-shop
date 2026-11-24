using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
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
            BackColor = Theme.Surface, // Match parent background
            Location = new Point(48, 12),
            Width = 200,
            Height = 24,
            UseSystemPasswordChar = isPassword,
            TabStop = true
        };        // Ensure TextBox background updates with parent
        _textBox.BackColor = _hasError ? Color.FromArgb(60, 30, 30) : (_focused ? Theme.SurfaceElevated : Theme.Surface);
        _textBox.TextChanged += (s, e) => { _hasError = false; Invalidate(); };
        _textBox.Enter += (s, e) => { _focused = true; _textBox.BackColor = Theme.SurfaceElevated; Invalidate(); };
        _textBox.Leave += (s, e) => { _focused = false; _textBox.BackColor = Theme.Surface; Invalidate(); };
        _textBox.KeyDown += (s, e) => OnKeyDown(e);
        
        Controls.Add(_textBox);
        _textBox.HandleCreated += (s, e) => ApplyCueBanner();
        ApplyCueBanner();

        // Set size AFTER _textBox is created to avoid OnResize accessing null _textBox
        Height = 48;
        MinimumSize = new Size(200, 48);
    }

    private const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    /// <summary>
    /// Placeholder рисуется самим TextBox: своя отрисовка под TextBox не видна,
    /// потому что TextBox непрозрачен и закрывает клиентскую область контрола.
    /// </summary>
    private void ApplyCueBanner()
    {
        if (!_textBox.IsHandleCreated) return;
        SendMessage(_textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyCueBanner();
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text 
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

        // Update TextBox background to match
        _textBox.BackColor = bgColor;

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

        // Placeholder теперь рисует сам TextBox (cue banner)

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
        _textBox.BackColor = _focused ? Theme.SurfaceElevated : Theme.Surface;
        Invalidate();
    }
}