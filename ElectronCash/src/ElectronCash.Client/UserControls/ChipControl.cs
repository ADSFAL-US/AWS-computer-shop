using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Чип (тег/метка) для отображения категорий, статусов и т.д.
/// </summary>
public class ChipControl : Control
{
    private string _text = string.Empty;
    private string _icon = string.Empty;
    private Color _backgroundColor = Theme.SurfaceElevated;
    private Color _textColor = Theme.TextPrimary;
    private bool _removable;
    private bool _removeHover;
    private bool _selected;

    public ChipControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Height = 28;
        MinimumSize = new Size(60, 28);
        Font = Theme.SmallFont;
        Cursor = Cursors.Hand;
    }

    /// <summary>Текст чипа.</summary>
    public string ChipText
    {
        get => _text;
        set { _text = value; UpdateSize(); Invalidate(); }
    }

    /// <summary>Иконка (Segoe MDL2 Assets Unicode).</summary>
    public string Icon
    {
        get => _icon;
        set { _icon = value; UpdateSize(); Invalidate(); }
    }

    /// <summary>Цвет фона.</summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; Invalidate(); }
    }

    /// <summary>Цвет текста.</summary>
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Invalidate(); }
    }

    /// <summary>Показывать кнопку удаления.</summary>
    public bool Removable
    {
        get => _removable;
        set { _removable = value; UpdateSize(); Invalidate(); }
    }

    /// <summary>Состояние выбора (для фильтров).</summary>
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    /// <summary>Событие удаления чипа.</summary>
    public event EventHandler? RemoveClicked;

    private void UpdateSize()
    {
        using (var g = CreateGraphics())
        {
            int textWidth = string.IsNullOrEmpty(_text) ? 0 : (int)g.MeasureString(_text, Font).Width;
            int iconWidth = string.IsNullOrEmpty(_icon) ? 0 : 20;
            int removeWidth = _removable ? 24 : 0;
            int padding = 24;
            int gap = string.IsNullOrEmpty(_icon) || string.IsNullOrEmpty(_text) ? 0 : 6;
            
            Width = padding + iconWidth + gap + textWidth + removeWidth;
        }
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
        Color bgColor = _selected ? Theme.Accent : _backgroundColor;
        Color txtColor = _selected ? Theme.Background : _textColor;

        using (var path = Theme.RoundedRect(rect, 14))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            // Тонкая рамка для невыбранных
            if (!_selected)
            {
                using (var pen = new Pen(Theme.Border, 1))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        int x = 12;
        int centerY = rect.Y + rect.Height / 2;

        // Иконка
        if (!string.IsNullOrEmpty(_icon))
        {
            using (var iconFont = new Font(Theme.IconFontFamily, 12))
            using (var brush = new SolidBrush(txtColor))
            {
                var iconRect = new Rectangle(x, centerY - 10, 20, 20);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_icon, iconFont, brush, iconRect, sf);
            }
            x += 20 + 6;
        }

        // Текст
        if (!string.IsNullOrEmpty(_text))
        {
            using (var brush = new SolidBrush(txtColor))
            {
                var textRect = new Rectangle(x, rect.Y, Width - x - (_removable ? 24 : 12), rect.Height);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                g.DrawString(_text, Font, brush, textRect, sf);
            }
        }

        // Кнопка удаления
        if (_removable)
        {
            var removeRect = new Rectangle(Width - 28, centerY - 10, 20, 20);
            Color removeBg = _removeHover ? Theme.Danger : Color.Transparent;
            Color removeIconColor = _removeHover ? Color.White : txtColor;

            if (removeBg != Color.Transparent)
            {
                using (var path = Theme.RoundedRect(removeRect, 10))
                using (var brush = new SolidBrush(removeBg))
                {
                    g.FillPath(brush, path);
                }
            }

            using (var iconFont = new Font(Theme.IconFontFamily, 10))
            using (var brush = new SolidBrush(removeIconColor))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("\uE711", iconFont, brush, removeRect, sf); // close
            }
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (_removable)
        {
            var removeRect = new Rectangle(Width - 28, Height / 2 - 10, 20, 20);
            if (removeRect.Contains(PointToClient(Cursor.Position)))
            {
                _removeHover = true;
                Invalidate();
            }
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _removeHover = false;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        
        if (_removable)
        {
            var removeRect = new Rectangle(Width - 28, Height / 2 - 10, 20, 20);
            if (removeRect.Contains(e.Location))
            {
                RemoveClicked?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}