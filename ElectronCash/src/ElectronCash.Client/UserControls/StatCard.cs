using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Карточка статистики с заголовком, значением и иконкой.
/// </summary>
public class StatCard : Control
{
    private string _title = string.Empty;
    private string _value = string.Empty;
    private string _icon = string.Empty;
    private Color _accentColor = Theme.Accent;

    public StatCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Size = new Size(180, 100);
        MinimumSize = new Size(150, 80);
        Font = Theme.RegularFont;
    }

    /// <summary>Заголовок карточки.</summary>
    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    /// <summary>Значение (крупные цифры).</summary>
    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    /// <summary>Иконка (Segoe MDL2 Assets Unicode).</summary>
    public string Icon
    {
        get => _icon;
        set { _icon = value; Invalidate(); }
    }

    /// <summary>Акцентный цвет для иконки и акцентной полоски.</summary>
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
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
        using (var path = Theme.RoundedRect(rect, 12))
        {
            using (var brush = new SolidBrush(Theme.Surface))
            {
                g.FillPath(brush, path);
            }

            // Тонкая рамка
            using (var pen = new Pen(Theme.Border, 1))
            {
                g.DrawPath(pen, path);
            }
        }

        // Акцентная полоска сверху
        var barRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 4);
        using (var barPath = new GraphicsPath())
        {
            barPath.AddArc(barRect.X, barRect.Y, 8, 8, 180, 90);
            barPath.AddArc(barRect.X + barRect.Width - 8, barRect.Y, 8, 8, 270, 90);
            barPath.AddLine(barRect.X + barRect.Width, barRect.Y + 4, barRect.X, barRect.Y + 4);
            barPath.CloseFigure();
            
            using (var brush = new SolidBrush(_accentColor))
            {
                g.FillPath(brush, barPath);
            }
        }

        // Иконка
        if (!string.IsNullOrEmpty(_icon))
        {
            using (var iconFont = new Font("Segoe MDL2 Assets", 24))
            using (var brush = new SolidBrush(_accentColor))
            {
                var iconRect = new Rectangle(rect.X + 16, rect.Y + 16, 40, 40);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_icon, iconFont, brush, iconRect, sf);
            }
        }

        // Значение (крупно)
        if (!string.IsNullOrEmpty(_value))
        {
            using (var brush = new SolidBrush(Theme.TextPrimary))
            {
                var valueRect = new Rectangle(rect.X + 16, rect.Y + 50, rect.Width - 32, 36);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                g.DrawString(_value, Theme.LargeNumberFont, brush, valueRect, sf);
            }
        }

        // Заголовок
        if (!string.IsNullOrEmpty(_title))
        {
            using (var brush = new SolidBrush(Theme.TextSecondary))
            {
                var titleRect = new Rectangle(rect.X + 16, rect.Y + 78, rect.Width - 32, 20);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_title, Theme.SmallFont, brush, titleRect, sf);
            }
        }
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}