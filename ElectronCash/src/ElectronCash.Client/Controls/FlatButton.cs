using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Варианты стиля кнопки.
/// </summary>
public enum ButtonVariant
{
    Primary,      // Accent background
    Secondary,    // SurfaceElevated background
    Ghost,        // Transparent background
    Danger        // Red background
}

/// <summary>
/// Кастомная кнопка с иконкой, текстом и поддержкой состояний hover/pressed/loading.
/// </summary>
public class FlatButton : Control
{
    private ButtonVariant _variant = ButtonVariant.Primary;
    private string _icon = string.Empty;
    private bool _loading;
    private bool _hover;
    private bool _pressed;
    private float _loadingAngle;

    public FlatButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Font = Theme.RegularFont;
        Height = 40;
        MinimumSize = new Size(80, 40);
        Padding = new Padding(16, 0, 16, 0);
    }

    /// <summary>Текст кнопки.</summary>
    public override string? Text { get => base.Text; set { base.Text = value ?? string.Empty; Invalidate(); } }

    /// <summary>Иконка (Segoe MDL2 Assets Unicode).</summary>
    public string Icon
    {
        get => _icon;
        set { _icon = value ?? string.Empty; Invalidate(); }
    }

    /// <summary>Вариант стиля кнопки.</summary>
    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    /// <summary>Состояние загрузки (показывает спиннер вместо текста).</summary>
    public bool Loading
    {
        get => _loading;
        set
        {
            _loading = value;
            Enabled = !value;
            if (value)
            {
                _loadingTimer.Start();
            }
            else
            {
                _loadingTimer.Stop();
            }
            Invalidate();
        }
    }

    private readonly System.Windows.Forms.Timer _loadingTimer = new() { Interval = 16 };

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _loadingTimer.Tick += (s, e) =>
        {
            _loadingAngle += 10f;
            if (_loadingAngle >= 360f) _loadingAngle = 0f;
            Invalidate();
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        var radius = 8;

        // Цвета в зависимости от варианта и состояния
        Color bgColor, textColor, borderColor;

        switch (_variant)
        {
            case ButtonVariant.Primary:
                bgColor = _pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent;
                textColor = Theme.Background;
                borderColor = Color.Transparent;
                break;
            case ButtonVariant.Secondary:
                bgColor = _pressed ? Theme.SurfaceHover : _hover ? Theme.SurfaceHover : Theme.SurfaceElevated;
                textColor = Theme.TextPrimary;
                borderColor = Theme.Border;
                break;
            case ButtonVariant.Ghost:
                bgColor = _pressed ? Theme.SurfaceHover : _hover ? Theme.SurfaceElevated : Color.Transparent;
                textColor = Theme.TextPrimary;
                borderColor = Color.Transparent;
                break;
            case ButtonVariant.Danger:
                bgColor = _pressed ? Color.FromArgb(180, 50, 50) : _hover ? Theme.Danger : Color.FromArgb(200, 80, 80);
                textColor = Color.White;
                borderColor = Color.Transparent;
                break;
            default:
                bgColor = Theme.Accent;
                textColor = Theme.Background;
                borderColor = Color.Transparent;
                break;
        }

        // Фон кнопки
        using (var path = Theme.RoundedRect(rect, radius))
        {
            if (bgColor != Color.Transparent)
            {
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }
            }

            // Рамка для Secondary
            if (borderColor != Color.Transparent)
            {
                using (var pen = new Pen(borderColor, 1))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        // Контент
        if (_loading)
        {
            DrawSpinner(g, rect);
        }
        else
        {
            DrawContent(g, rect, textColor);
        }
    }

    private void DrawContent(Graphics g, Rectangle rect, Color textColor)
    {
        int iconWidth = string.IsNullOrEmpty(_icon) ? 0 : 20;
        int gap = string.IsNullOrEmpty(_icon) || string.IsNullOrEmpty(Text) ? 0 : 8;
        int textWidth = string.IsNullOrEmpty(Text) ? 0 : (int)g.MeasureString(Text, Font).Width;
        int totalWidth = iconWidth + gap + textWidth;

        int startX = (rect.Width - totalWidth) / 2 + rect.X;
        int centerY = rect.Y + rect.Height / 2;

        // Иконка
        if (!string.IsNullOrEmpty(_icon))
        {
            using (var iconFont = new Font("Segoe MDL2 Assets", 14))
            using (var brush = new SolidBrush(textColor))
            {
                var iconRect = new Rectangle(startX, centerY - 10, 20, 20);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_icon, iconFont, brush, iconRect, sf);
            }
            startX += iconWidth + gap;
        }

        // Текст
        if (!string.IsNullOrEmpty(Text))
        {
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(startX, rect.Y, textWidth, rect.Height);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString(Text, Font, brush, textRect, sf);
            }
        }
    }

    private void DrawSpinner(Graphics g, Rectangle rect)
    {
        int size = Math.Min(rect.Width, rect.Height) - 16;
        int cx = rect.X + rect.Width / 2;
        int cy = rect.Y + rect.Height / 2;

        using (var pen = new Pen(Theme.Background, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            var rectSpinner = new Rectangle(cx - size / 2, cy - size / 2, size, size);
            g.DrawArc(pen, rectSpinner, _loadingAngle, 270);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && Enabled)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    /// <summary>
    /// Применяет тему (для совместимости с ModernForm.ApplyTheme).
    /// </summary>
    public void ApplyTheme()
    {
        Font = Theme.RegularFont;
        Invalidate();
    }
}