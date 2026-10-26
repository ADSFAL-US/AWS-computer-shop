using System.Drawing.Drawing2D;
using ElectronCash.Client.Theme;

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
/// </summary>
public class Toast : Form
{
    private static readonly Queue<Toast> _toastQueue = new();
    private static readonly object _queueLock = new();
    private const int ToastWidth = 360;
    private const int ToastHeight = 72;
    private const int Margin = 16;
    private const int Gap = 8;

    private readonly ToastType _type;
    private readonly string _message;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private readonly System.Windows.Forms.Timer _autoCloseTimer;
    private float _opacity = 0f;
    private bool _fadingIn = true;
    private bool _fadingOut = false;

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

            using (var iconFont = new Font("Segoe MDL2 Assets", 18))
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
                ProcessQueue();
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
    /// Показать уведомление.
    /// </summary>
    public static void Show(Form owner, string message, ToastType type = ToastType.Info)
    {
        var toast = new Toast(type, message);
        
        lock (_queueLock)
        {
            // Позиционируем относительно владельца
            int x = owner.Right - ToastWidth - Margin;
            int y = owner.Top + Margin;
            
            // Смещаем вниз если есть другие тосты
            foreach (var existing in _toastQueue)
            {
                y += existing.Height + Gap;
            }

            toast.Location = new Point(x, y);
            _toastQueue.Enqueue(toast);
            
            if (_toastQueue.Count == 1)
            {
                toast.Show(owner);
            }
        }
    }

    private static void ProcessQueue()
    {
        lock (_queueLock)
        {
            _toastQueue.Dequeue();
            
            if (_toastQueue.Count > 0)
            {
                var next = _toastQueue.Peek();
                // Анимируем подъём остальных тостов
                AnimateQueueShift(next);
            }
        }
    }

    private static void AnimateQueueShift(Toast nextToast)
    {
        // Просто показываем следующий
        nextToast.Show(nextToast.Owner);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _fadeTimer?.Dispose();
        _autoCloseTimer?.Dispose();
        base.OnFormClosed(e);
    }
}