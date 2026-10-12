using System.Drawing.Drawing2D;

namespace ElectronCash.Client.Theme;

/// <summary>
/// Статический класс с общей цветовой палитрой, шрифтами и вспомогательными методами для кастомной отрисовки.
/// </summary>
public static class Theme
{
    // ============= ЦВЕТА =============
    public static readonly Color Background = Color.FromArgb(15, 20, 25);           // #0F1419
    public static readonly Color Surface = Color.FromArgb(26, 33, 41);              // #1A2129
    public static readonly Color SurfaceElevated = Color.FromArgb(35, 43, 54);      // #232B36
    public static readonly Color SurfaceHover = Color.FromArgb(42, 52, 65);         // #2A3441
    public static readonly Color Border = Color.FromArgb(46, 56, 68);               // #2E3844
    
    public static readonly Color TextPrimary = Color.FromArgb(240, 244, 248);       // #F0F4F8
    public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);     // #94A3B8
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);         // #64748B
    
    public static readonly Color Accent = Color.FromArgb(0, 217, 163);              // #00D9A3
    public static readonly Color AccentHover = Color.FromArgb(0, 194, 146);         // #00C292
    public static readonly Color AccentPressed = Color.FromArgb(0, 168, 125);       // #00A87D
    
    public static readonly Color Danger = Color.FromArgb(248, 113, 113);            // #F87171
    public static readonly Color Warning = Color.FromArgb(251, 191, 36);            // #FBBF24
    public static readonly Color Success = Color.FromArgb(52, 211, 153);            // #34D399
    public static readonly Color Info = Color.FromArgb(96, 165, 250);               // #60A5FA

    // ============= ШРИФТЫ =============
    private static readonly string FontFamily = "Segoe UI";
    
    /// <summary>Заголовок формы: 20pt SemiBold</summary>
    public static Font FormTitleFont => new Font(FontFamily, 20, FontStyle.Bold);
    
    /// <summary>Заголовок секции: 12pt SemiBold</summary>
    public static Font SectionTitleFont => new Font(FontFamily, 12, FontStyle.Bold);
    
    /// <summary>Основной текст: 10pt Regular</summary>
    public static Font RegularFont => new Font(FontFamily, 10);
    
    /// <summary>Мелкий текст: 9pt Regular</summary>
    public static Font SmallFont => new Font(FontFamily, 9);
    
    /// <summary>Крупные цифры для StatCard: 28pt SemiBold</summary>
    public static Font LargeNumberFont => new Font(FontFamily, 28, FontStyle.Bold);
    
    /// <summary>Название товара: 11pt SemiBold</summary>
    public static Font ProductNameFont => new Font(FontFamily, 11, FontStyle.Bold);

    // ============= МЕТОДЫ =============
    
    /// <summary>
    /// Создаёт GraphicsPath с закруглёнными углами.
    /// </summary>
    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        
        // Углы
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90);
        
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Рисует мягкую тень под элементом.
    /// </summary>
    public static void DrawShadow(Graphics g, Rectangle rect, int offsetY = 4, int blurRadius = 12)
    {
        // Рисуем несколько полупрозрачных слоёв для эффекта размытия
        for (int i = blurRadius; i > 0; i--)
        {
            int alpha = (int)(255 * 0.3 * (1 - (float)i / blurRadius) / 2);
            if (alpha <= 0) continue;
            
            using (var shadowBrush = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
            using (var path = RoundedRect(
                new Rectangle(rect.X - i/2, rect.Y + offsetY - i/2, rect.Width + i, rect.Height + i), 8))
            {
                g.FillPath(shadowBrush, path);
            }
        }
    }

    /// <summary>
    /// Рисует текст с выравниванием и обрезкой.
    /// </summary>
    public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle rect, 
        StringAlignment horizontalAlign = StringAlignment.Near, StringAlignment verticalAlign = StringAlignment.Near)
    {
        var stringFormat = new StringFormat
        {
            Alignment = horizontalAlign,
            LineAlignment = verticalAlign,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        using (var brush = new SolidBrush(color))
        {
            g.DrawString(text, font, brush, rect, stringFormat);
        }
    }

    /// <summary>
    /// Рисует градиент вертикально.
    /// </summary>
    public static Brush CreateVerticalGradient(Rectangle rect, Color colorStart, Color colorEnd)
    {
        return new LinearGradientBrush(
            new Point(rect.Left, rect.Top),
            new Point(rect.Left, rect.Bottom),
            colorStart,
            colorEnd);
    }

    /// <summary>
    /// Рисует градиент горизонтально.
    /// </summary>
    public static Brush CreateHorizontalGradient(Rectangle rect, Color colorStart, Color colorEnd)
    {
        return new LinearGradientBrush(
            new Point(rect.Left, rect.Top),
            new Point(rect.Right, rect.Top),
            colorStart,
            colorEnd);
    }
}
