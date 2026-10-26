using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ElectronCash.Client.Theming;
using ElectronCash.Client.UserControls;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Базовая форма с кастомным заголовком, скруглёнными углами и тёмной темой.
/// </summary>
public class ModernForm : Form
{
    private const int TitleBarHeight = 40;
    private const int CornerRadius = 12;
    private const int ButtonSize = 36;
    private const int ButtonMargin = 8;

    private Rectangle _closeButtonRect;
    private Rectangle _maximizeButtonRect;
    private Rectangle _minimizeButtonRect;
    private bool _closeHover, _maximizeHover, _minimizeHover;
    private bool _closePressed, _maximizePressed, _minimizePressed;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    public ModernForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font = Theme.RegularFont;
        DoubleBuffered = true;
        MinimumSize = new Size(400, 300);
        Padding = new Padding(0, TitleBarHeight, 0, 0);
        
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyTheme();
        UpdateRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var clientRect = ClientRectangle;

        // Фон формы
        using (var bgBrush = new SolidBrush(Theme.Background))
        {
            g.FillPath(bgBrush, Theme.RoundedRect(clientRect, CornerRadius));
        }

        // Тень (рисуем за формой)
        Theme.DrawShadow(g, clientRect);

        // Заголовок
        DrawTitleBar(g);

        // Кнопки управления окном
        DrawWindowButtons(g);
    }

    private void DrawTitleBar(Graphics g)
    {
        var titleRect = new Rectangle(0, 0, Width, TitleBarHeight);
        
        using (var path = Theme.RoundedRect(titleRect, CornerRadius))
        {
            // Обрезаем нижние углы заголовка
            var clipRect = new Rectangle(0, 0, Width, TitleBarHeight);
            var clipPath = new GraphicsPath();
            clipPath.AddRectangle(clipRect);
            g.SetClip(clipPath);
            
            using (var brush = new SolidBrush(Theme.SurfaceElevated))
            {
                g.FillPath(brush, path);
            }
            g.ResetClip();
        }

        // Текст заголовка
        var textRect = new Rectangle(16, 0, Width - 120, TitleBarHeight);
        Theme.DrawText(g, Text, Theme.SectionTitleFont, Theme.TextPrimary, textRect, StringAlignment.Near, StringAlignment.Center);
    }

    private void DrawWindowButtons(Graphics g)
    {
        int right = Width - ButtonMargin;
        int top = (TitleBarHeight - ButtonSize) / 2;

        // Close button
        _closeButtonRect = new Rectangle(right - ButtonSize, top, ButtonSize, ButtonSize);
        DrawWindowButton(g, _closeButtonRect, "\uE8BB", _closeHover, _closePressed, Theme.Danger);

        // Maximize button
        right -= ButtonSize + 4;
        _maximizeButtonRect = new Rectangle(right - ButtonSize, top, ButtonSize, ButtonSize);
        DrawWindowButton(g, _maximizeButtonRect, WindowState == FormWindowState.Maximized ? "\uE923" : "\uE922", _maximizeHover, _maximizePressed);

        // Minimize button
        right -= ButtonSize + 4;
        _minimizeButtonRect = new Rectangle(right - ButtonSize, top, ButtonSize, ButtonSize);
        DrawWindowButton(g, _minimizeButtonRect, "\uE921", _minimizeHover, _minimizePressed);
    }

    private void DrawWindowButton(Graphics g, Rectangle rect, string icon, bool hover, bool pressed, Color? hoverColor = null)
    {
        Color bgColor;
        if (pressed)
            bgColor = hoverColor ?? Theme.SurfaceHover;
        else if (hover)
            bgColor = hoverColor ?? Theme.SurfaceHover;
        else
            bgColor = Color.Transparent;

        if (bgColor != Color.Transparent)
        {
            using (var brush = new SolidBrush(bgColor))
            using (var path = Theme.RoundedRect(rect, 6))
            {
                g.FillPath(brush, path);
            }
        }

        // Иконка
        var iconColor = hoverColor.HasValue && (hover || pressed) ? Color.White : Theme.TextSecondary;
        if (hoverColor.HasValue && hoverColor.Value == Theme.Danger && (hover || pressed))
            iconColor = Color.White;

        using (var font = new Font("Segoe MDL2 Assets", 12))
        using (var brush = new SolidBrush(iconColor))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(icon, font, brush, rect, sf);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left && e.Y < TitleBarHeight)
        {
            if (_closeButtonRect.Contains(e.Location))
            {
                _closePressed = true;
                Invalidate(_closeButtonRect);
            }
            else if (_maximizeButtonRect.Contains(e.Location))
            {
                _maximizePressed = true;
                Invalidate(_maximizeButtonRect);
            }
            else if (_minimizeButtonRect.Contains(e.Location))
            {
                _minimizePressed = true;
                Invalidate(_minimizeButtonRect);
            }
            else
            {
                // Drag window
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Left)
        {
            if (_closePressed && _closeButtonRect.Contains(e.Location))
            {
                Close();
            }
            else if (_maximizePressed && _maximizeButtonRect.Contains(e.Location))
            {
                WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                UpdateRegion();
            }
            else if (_minimizePressed && _minimizeButtonRect.Contains(e.Location))
            {
                WindowState = FormWindowState.Minimized;
            }

            _closePressed = _maximizePressed = _minimizePressed = false;
            Invalidate(new Rectangle(Width - 120, 0, 120, TitleBarHeight));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool closeHover = _closeButtonRect.Contains(e.Location);
        bool maximizeHover = _maximizeButtonRect.Contains(e.Location);
        bool minimizeHover = _minimizeButtonRect.Contains(e.Location);

        if (closeHover != _closeHover || maximizeHover != _maximizeHover || minimizeHover != _minimizeHover)
        {
            _closeHover = closeHover;
            _maximizeHover = maximizeHover;
            _minimizeHover = minimizeHover;
            Invalidate(new Rectangle(Width - 120, 0, 120, TitleBarHeight));
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _closeHover = _maximizeHover = _minimizeHover = false;
        Invalidate(new Rectangle(Width - 120, 0, 120, TitleBarHeight));
    }

    private void UpdateRegion()
    {
        if (Width > 0 && Height > 0)
        {
            using (var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region = new Region(path);
            }
        }
    }

    /// <summary>
    /// Рекурсивно применяет тему ко всем контролам.
    /// </summary>
    public void ApplyTheme()
    {
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font = Theme.RegularFont;

        foreach (Control ctrl in Controls)
        {
            ApplyThemeToControl(ctrl);
        }
    }

    private void ApplyThemeToControl(Control ctrl)
    {
        if (ctrl is FlatButton btn)
        {
            btn.ApplyTheme();
        }
        else if (ctrl is CardPanel card)
        {
            card.ApplyTheme();
        }
        else if (ctrl is SearchBox search)
        {
            search.ApplyTheme();
        }
        else if (ctrl is StatCard stat)
        {
            stat.ApplyTheme();
        }
        else if (ctrl is ProductTile tile)
        {
            tile.ApplyTheme();
        }
        else if (ctrl is CartRow row)
        {
            row.ApplyTheme();
        }
        else if (ctrl is ChipControl chip)
        {
            chip.ApplyTheme();
        }
        else if (ctrl is ProgressBarCustom progress)
        {
            progress.ApplyTheme();
        }
        else
        {
            ctrl.BackColor = Theme.Background;
            ctrl.ForeColor = Theme.TextPrimary;
            ctrl.Font = Theme.RegularFont;
        }

        foreach (Control child in ctrl.Controls)
        {
            ApplyThemeToControl(child);
        }
    }
}