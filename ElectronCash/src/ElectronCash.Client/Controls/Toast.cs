using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Тип уведомления.
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Всплывающее уведомление (Toast) с анимацией fade-in/out.
/// Несколько уведомлений показываются одновременно и складываются в стопку.
/// </summary>
public class Toast : Form
{
    /// <summary>Активные тосты, индекс 0 — самый нижний (новый).</summary>
    private static readonly List<Toast> _active = new();
    private static readonly object _sync = new();
    private const int ToastWidth = 360;
    private const int ToastHeight = 72;
    private const int MaxVisible = 3;
    private new const int Margin = 16;
    private const int Gap = 8;

    private readonly ToastType _type;
    private readonly string _message;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private float _opacity = 0f;
    private bool _fadingIn = true;
    private bool _fadingOut = false;
    private Form? _screenAreaOwner;
    private EventHandler? _ownerMovedHandler;
    private bool _closedNotified;

    private Toast(ToastType type, string message)
    {
        _type = type;
        _message = message;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(ToastWidth, ToastHeight);
        BackColor = Theme.SurfaceElevated;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += FadeTimer_Tick;

        _autoCloseTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _autoCloseTimer.Tick += (s, e) => StartFadeOut();
    }

    protected override bool ShowWithoutActivation => true;

    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x80;

    /// <summary>Тост не должен забирать клики и фокус у окна под ним.</summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Opacity = 0;
        _fadeTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Тень
        Theme.DrawShadow(g, rect, 4, 16);

        // Фон с закруглением
        using (var path = Theme.RoundedRect(rect, 12))
        {
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), Theme.SurfaceElevated)))
            {
                g.FillPath(brush, path);
            }

            // Цветная полоска слева
            var accentColor = _type switch
            {
                ToastType.Success => Theme.Success,
                ToastType.Error => Theme.Danger,
                ToastType.Warning => Theme.Warning,
                ToastType.Info => Theme.Info,
                _ => Theme.Accent
            };

            var barRect = new Rectangle(rect.X, rect.Y, 4, rect.Height);
            using (var barPath = new GraphicsPath())
            {
                barPath.AddArc(barRect.X, barRect.Y, 8, 8, 180, 90);
                barPath.AddArc(barRect.X, barRect.Y + barRect.Height - 8, 8, 8, 90, 90);
                barPath.CloseFigure();
                using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), accentColor)))
                {
                    g.FillPath(brush, barPath);
                }
            }

            // Иконка
            string icon = _type switch
            {
                ToastType.Success => "\uE73E", // галочка
                ToastType.Error => "\uE711",   // крест
                ToastType.Warning => "\uE7BA", // восклицание
                ToastType.Info => "\uE946",    // i
                _ => "\uE73E"
            };

            using (var iconFont = new Font(Theme.IconFontFamily, 18))
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), accentColor)))
            {
                var iconRect = new Rectangle(rect.X + 16, rect.Y + 16, 40, 40);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(icon, iconFont, brush, iconRect, sf);
            }

            // Текст
            using (var brush = new SolidBrush(Color.FromArgb((int)(255 * _opacity), Theme.TextPrimary)))
            {
                var textRect = new Rectangle(rect.X + 60, rect.Y + 12, rect.Width - 80, rect.Height - 24);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_message, Theme.RegularFont, brush, textRect, sf);
            }
        }
    }

    private void FadeTimer_Tick(object? sender, EventArgs e)
    {
        if (_fadingIn)
        {
            _opacity += 0.1f;
            if (_opacity >= 1f)
            {
                _opacity = 1f;
                _fadingIn = false;
                _autoCloseTimer.Start();
            }
            Opacity = _opacity;
            Invalidate();
        }
        else if (_fadingOut)
        {
            _opacity -= 0.1f;
            if (_opacity <= 0f)
            {
                _opacity = 0f;
                _fadingOut = false;
                _fadeTimer.Stop();
                Close();
                return;
            }
            Opacity = _opacity;
            Invalidate();
        }
    }

    private void StartFadeOut()
    {
        _autoCloseTimer.Stop();
        _fadingOut = true;
    }

    /// <summary>
    /// Показать уведомление. Тост появляется сразу и не ждёт, пока исчезнут
    /// предыдущие: они просто стопкой поднимаются вверх.
    /// </summary>
    public static void Show(Form owner, string message, ToastType type = ToastType.Info)
    {
        var toast = new Toast(type, message) { _screenAreaOwner = owner };

        lock (_sync)
        {
            // Новый тост встаёт в самый низ стопки, старые поднимаются выше.
            _active.Insert(0, toast);

            // Больше MaxVisible одновременно не показываем: лишние старые тосты
            // убираем досрочно, иначе стопка уезжает за верхнюю границу экрана.
            if (_active.Count > MaxVisible)
            {
                var overflow = _active.Skip(MaxVisible).ToList();
                foreach (var extra in overflow)
                {
                    _active.Remove(extra);
                    extra._closedNotified = true;
                    extra.Dispose();
                }
            }

            RepositionAll();
        }

        toast.Show(owner);

        // Тост — отдельное окно: при перемещении/изменении размера окна
        // стопку нужно пересчитать, иначе уведомления останутся «в воздухе».
        if (toast._ownerMovedHandler == null)
        {
            toast._ownerMovedHandler = (s, e) =>
            {
                lock (_sync)
                    RepositionAll();
            };
            owner.LocationChanged += toast._ownerMovedHandler;
            owner.SizeChanged += toast._ownerMovedHandler;
        }
    }

    /// <summary>
    /// Левый нижний угол окна: правый занят кнопкой «Оформить заказ» в корзине.
    /// </summary>
    private static Point GetSlotLocation(Toast toast, int indexFromBottom)
    {
        var owner = toast._screenAreaOwner;
        if (owner == null || owner.IsDisposed)
            return toast.Location;

        var area = Screen.FromHandle(owner.Handle).WorkingArea;
        int baseX = owner.Left + Margin;
        int baseY = owner.Bottom - ToastHeight - Margin;

        int x = Math.Max(area.Left + Margin, Math.Min(baseX, area.Right - ToastWidth - Margin));
        int y = baseY - indexFromBottom * (ToastHeight + Gap);

        // Не даём стопке уехать за верхнюю границу экрана
        int minY = area.Top + Margin;
        if (y < minY) y = minY;

        return new Point(x, y);
    }

    /// <summary>Раскладывает активные тосты стопкой от нижнего края окна вверх.</summary>
    private static void RepositionAll()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            var toast = _active[i];
            if (toast.IsDisposed) continue;
            toast.Location = GetSlotLocation(toast, i);
        }
    }

    private static void Remove(Toast toast)
    {
        lock (_sync)
        {
            if (_active.Remove(toast))
                RepositionAll();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (!_closedNotified)
        {
            _closedNotified = true;
            Remove(this);
        }

        if (_ownerMovedHandler != null && _screenAreaOwner != null)
        {
            _screenAreaOwner.LocationChanged -= _ownerMovedHandler;
            _screenAreaOwner.SizeChanged -= _ownerMovedHandler;
            _ownerMovedHandler = null;
        }

        _fadeTimer?.Dispose();
        _autoCloseTimer?.Dispose();
        base.OnFormClosed(e);
    }
}