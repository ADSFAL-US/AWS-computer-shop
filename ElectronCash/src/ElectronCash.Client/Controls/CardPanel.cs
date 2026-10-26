using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Панель-карточка с фоном Surface, рамкой Border, скруглением и мягкой тенью.
/// </summary>
public class CardPanel : Panel
{
    private int _cornerRadius = 12;
    private bool _showShadow = true;
    private Color _borderColor = Theme.Border;

    public CardPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Surface;
        Padding = new Padding(16);
        Margin = new Padding(8);
    }

    /// <summary>Радиус скругления углов.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    /// <summary>Показывать ли тень.</summary>
    public bool ShowShadow
    {
        get => _showShadow;
        set { _showShadow = value; Invalidate(); }
    }

    /// <summary>Цвет рамки.</summary>
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Тень (рисуем первой, под карточкой)
        if (_showShadow)
        {
            Theme.DrawShadow(g, rect);
        }

        // Фон карточки
        using (var path = Theme.RoundedRect(rect, _cornerRadius))
        {
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            // Рамка
            using (var pen = new Pen(_borderColor, 1))
            {
                g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>
    /// Применяет тему (для совместимости с ModernForm.ApplyTheme).
    /// </summary>
    public void ApplyTheme()
    {
        BackColor = Theme.Surface;
        _borderColor = Theme.Border;
        Invalidate();
    }
}