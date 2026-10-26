using System.Drawing.Drawing2D;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Кастомный прогресс-бар с анимацией и поддержкой неопределенного состояния.
/// </summary>
public class ProgressBarCustom : Control
{
    private int _value = 0;
    private int _maximum = 100;
    private bool _indeterminate = false;
    private float _animationOffset = 0f;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public ProgressBarCustom()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Height = 8;
        MinimumSize = new Size(100, 8);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _animationTimer.Tick += (s, e) =>
        {
            if (_indeterminate)
            {
                _animationOffset += 0.05f;
                if (_animationOffset > 1f) _animationOffset = 0f;
                Invalidate();
            }
        };
    }

    /// <summary>Текущее значение прогресса.</summary>
    public int Value
    {
        get => _value;
        set 
        { 
            _value = Math.Clamp(value, 0, _maximum); 
            _indeterminate = false;
            _animationTimer.Stop();
            Invalidate(); 
        }
    }

    /// <summary>Максимальное значение.</summary>
    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(1, value); Invalidate(); }
    }

    /// <summary>Неопределенный прогресс (анимация).</summary>
    public bool Indeterminate
    {
        get => _indeterminate;
        set 
        { 
            _indeterminate = value; 
            if (value)
                _animationTimer.Start();
            else
                _animationTimer.Stop();
            Invalidate(); 
        }
    }

    /// <summary>Процент прогресса (0-100).</summary>
    public int Percent => _maximum > 0 ? (_value * 100 / _maximum) : 0;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Фон трека
        using (var path = Theme.RoundedRect(rect, 4))
        {
            using (var brush = new SolidBrush(Theme.Surface))
            {
                g.FillPath(brush, path);
            }
        }

        if (_indeterminate)
        {
            // Анимированная полоса для неопределенного прогресса
            int barWidth = (int)(rect.Width * 0.3);
            int barX = rect.X + (int)((rect.Width - barWidth) * _animationOffset);
            
            var barRect = new Rectangle(barX, rect.Y, barWidth, rect.Height);
            using (var path = Theme.RoundedRect(barRect, 4))
            {
                using (var brush = new SolidBrush(Theme.Accent))
                {
                    g.FillPath(brush, path);
                }
            }
        }
        else if (_value > 0)
        {
            // Обычный прогресс
            int fillWidth = (int)((float)_value / _maximum * rect.Width);
            var fillRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height);
            
            using (var path = Theme.RoundedRect(fillRect, 4))
            {
                // Обрезаем правый край если не заполнен полностью
                if (fillWidth < rect.Width)
                {
                    var clipRect = new Rectangle(fillRect.X, fillRect.Y, fillWidth, fillRect.Height);
                    var clipPath = new GraphicsPath();
                    clipPath.AddRectangle(clipRect);
                    g.SetClip(clipPath);
                }

                using (var brush = new SolidBrush(Theme.Accent))
                {
                    g.FillPath(brush, path);
                }
                g.ResetClip();
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}