using System.Drawing;
using System.Drawing.Drawing2D;

namespace ElectronCash.Client.Theming;

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
    private static readonly string[] FontFamilies = { "Segoe UI", "Arial", "Tahoma", "Microsoft Sans Serif" };
    private static readonly string[] IconFontFamilies = { "Segoe MDL2 Assets", "Segoe UI Symbol", "Noto Sans Symbols", "Symbola", "DejaVu Sans" };
    private static FontFamily? _fontFamilyInstance;
    private static FontFamily? _iconFontFamilyInstance;
    private static readonly object _fontFamilyLock = new();
    
    public static FontFamily FontFamily
    {
        get
        {
            if (_fontFamilyInstance != null) return _fontFamilyInstance;
            lock (_fontFamilyLock)
            {
                if (_fontFamilyInstance != null) return _fontFamilyInstance;
                
                foreach (var name in FontFamilies)
                {
                    try 
                    { 
                        _fontFamilyInstance = new FontFamily(name);
                        return _fontFamilyInstance;
                    }
                    catch { }
                }
                try 
                { 
                    _fontFamilyInstance = FontFamily.GenericSansSerif; 
                }
                catch 
                { 
                    _fontFamilyInstance = new FontFamily("Arial"); 
                }
                return _fontFamilyInstance;
            }
        }
    }
    
    public static FontFamily IconFontFamily
    {
        get
        {
            if (_iconFontFamilyInstance != null) return _iconFontFamilyInstance;
            lock (_fontFamilyLock)
            {
                if (_iconFontFamilyInstance != null) return _iconFontFamilyInstance;
                
                foreach (var name in IconFontFamilies)
                {
                    try 
                    { 
                        _iconFontFamilyInstance = new FontFamily(name);
                        return _iconFontFamilyInstance;
                    }
                    catch { }
                }
                try 
                { 
                    _iconFontFamilyInstance = FontFamily.GenericSansSerif; 
                }
                catch 
                { 
                    _iconFontFamilyInstance = new FontFamily("Arial"); 
                }
                return _iconFontFamilyInstance;
            }
        }
    }
    
    private static Font? _formTitleFont;
    private static Font? _sectionTitleFont;
    private static Font? _regularFont;
    private static Font? _smallFont;
    private static Font? _largeNumberFont;
    private static Font? _productNameFont;
    private static readonly object _fontLock = new();
    
    /// <summary>Заголовок формы: 20pt SemiBold</summary>
    public static Font FormTitleFont
    {
        get
        {
            if (_formTitleFont != null) return _formTitleFont;
            lock (_fontLock)
            {
                if (_formTitleFont != null) return _formTitleFont;
                try { _formTitleFont = new Font(FontFamily, 20, FontStyle.Bold); }
                catch { _formTitleFont = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold); }
                return _formTitleFont;
            }
        }
    }
    
    /// <summary>Заголовок секции: 12pt SemiBold</summary>
    public static Font SectionTitleFont
    {
        get
        {
            if (_sectionTitleFont != null) return _sectionTitleFont;
            lock (_fontLock)
            {
                if (_sectionTitleFont != null) return _sectionTitleFont;
                try { _sectionTitleFont = new Font(FontFamily, 12, FontStyle.Bold); }
                catch { _sectionTitleFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold); }
                return _sectionTitleFont;
            }
        }
    }
    
    /// <summary>Основной текст: 10pt Regular</summary>
    public static Font RegularFont
    {
        get
        {
            if (_regularFont != null) return _regularFont;
            lock (_fontLock)
            {
                if (_regularFont != null) return _regularFont;
                try { _regularFont = new Font(FontFamily, 10); }
                catch { _regularFont = new Font(FontFamily.GenericSansSerif, 10); }
                return _regularFont;
            }
        }
    }
    
    /// <summary>Мелкий текст: 9pt Regular</summary>
    public static Font SmallFont
    {
        get
        {
            if (_smallFont != null) return _smallFont;
            lock (_fontLock)
            {
                if (_smallFont != null) return _smallFont;
                try { _smallFont = new Font(FontFamily, 9); }
                catch { _smallFont = new Font(FontFamily.GenericSansSerif, 9); }
                return _smallFont;
            }
        }
    }
    
    /// <summary>Крупные цифры для StatCard: 28pt SemiBold</summary>
    public static Font LargeNumberFont
    {
        get
        {
            if (_largeNumberFont != null) return _largeNumberFont;
            lock (_fontLock)
            {
                if (_largeNumberFont != null) return _largeNumberFont;
                try { _largeNumberFont = new Font(FontFamily, 28, FontStyle.Bold); }
                catch { _largeNumberFont = new Font(FontFamily.GenericSansSerif, 28, FontStyle.Bold); }
                return _largeNumberFont;
            }
        }
    }
    
    /// <summary>Название товара: 11pt SemiBold</summary>
    public static Font ProductNameFont
    {
        get
        {
            if (_productNameFont != null) return _productNameFont;
            lock (_fontLock)
            {
                if (_productNameFont != null) return _productNameFont;
                try { _productNameFont = new Font(FontFamily, 11, FontStyle.Bold); }
                catch { _productNameFont = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold); }
                return _productNameFont;
            }
        }
    }

    // ============= МЕТОДЫ =============
    
    /// <summary>
    /// Создаёт GraphicsPath с закруглёнными углами.
    /// </summary>
    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        try
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
        catch
        {
            // Fallback: simple rectangle
            var path = new GraphicsPath();
            path.AddRectangle(rect);
            return path;
        }
    }

    /// <summary>
    /// Рисует мягкую тень под элементом.
    /// </summary>
    public static void DrawShadow(Graphics g, Rectangle rect, int offsetY = 4, int blurRadius = 12)
    {
        if (g == null) return;
        
        try
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
        catch
        {
            // Ignore drawing errors
        }
    }

    /// <summary>
    /// Рисует текст с выравниванием и обрезкой.
    /// </summary>
    public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle rect, 
        StringAlignment horizontalAlign = StringAlignment.Near, StringAlignment verticalAlign = StringAlignment.Near)
    {
        if (g == null || font == null || string.IsNullOrEmpty(text)) return;
        
        try
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
        catch
        {
            // Ignore drawing errors
        }
    }

    /// <summary>
    /// Рисует градиент вертикально.
    /// </summary>
    public static Brush CreateVerticalGradient(Rectangle rect, Color colorStart, Color colorEnd)
    {
        try
        {
            return new LinearGradientBrush(
                new Point(rect.Left, rect.Top),
                new Point(rect.Left, rect.Bottom),
                colorStart,
                colorEnd);
        }
        catch
        {
            return new SolidBrush(colorStart);
        }
    }

    /// <summary>
    /// Рисует градиент горизонтально.
    /// </summary>
    public static Brush CreateHorizontalGradient(Rectangle rect, Color colorStart, Color colorEnd)
    {
        try
        {
            return new LinearGradientBrush(
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                colorStart,
                colorEnd);
        }
        catch
        {
            return new SolidBrush(colorStart);
        }
    }
}
