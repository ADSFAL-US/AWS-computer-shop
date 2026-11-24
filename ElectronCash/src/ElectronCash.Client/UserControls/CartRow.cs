using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Models;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Строка товара в корзине. Раскладка пересчитывается от текущей ширины,
/// поэтому строка тянется вместе с боковой панелью.
/// </summary>
public class CartRow : Control
{
    private const int ContentPadding = 16;
    private const int ButtonSize = 36;
    private const int ButtonGap = 4;

    private OrderItem? _item;
    private bool _hover;
    private bool _qtyMinusPressed;
    private bool _qtyPlusPressed;
    private bool _removePressed;

    public CartRow()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Height = 100;
        MinimumSize = new Size(240, 100);
        Font = Theme.RegularFont;
    }

    /// <summary>Элемент заказа для отображения.</summary>
    public OrderItem? Item
    {
        get => _item;
        set { _item = value; Invalidate(); }
    }

    /// <summary>Сколько единиц товара есть на складе (0 — лимит неизвестен).</summary>
    public int MaxQuantity { get; set; }

    /// <summary>Событие изменения количества.</summary>
    public event EventHandler<int>? QuantityChanged;

    /// <summary>Событие удаления товара.</summary>
    public event EventHandler? RemoveClicked;

    private int ButtonY => (Height - ButtonSize) / 2;

    private Rectangle RemoveRect => new(Width - ContentPadding - ButtonSize, ButtonY, ButtonSize, ButtonSize);

    private Rectangle PlusRect => new(RemoveRect.Left - ButtonGap - ButtonSize, ButtonY, ButtonSize, ButtonSize);

    private Rectangle QtyRect => new(PlusRect.Left - 40, ButtonY, 40, ButtonSize);

    private Rectangle MinusRect => new(QtyRect.Left - ButtonGap - ButtonSize, ButtonY, ButtonSize, ButtonSize);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        Color bgColor = _hover ? Theme.SurfaceHover : Theme.Surface;

        using (var path = Theme.RoundedRect(rect, 8))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }
            using (var pen = new Pen(Theme.Border, 1))
            {
                g.DrawPath(pen, path);
            }
        }

        if (_item == null) return;

        int textWidth = Math.Max(40, MinusRect.Left - ContentPadding - 8);

        var nameRect = new Rectangle(rect.X + ContentPadding, rect.Y + 10, textWidth, 24);
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(_item.ProductName, Theme.RegularFont, brush, nameRect, sf);
        }

        var priceRect = new Rectangle(rect.X + ContentPadding, rect.Y + 36, textWidth, 20);
        using (var brush = new SolidBrush(Theme.TextSecondary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            g.DrawString($"{_item.UnitPrice:N0} ₽ × {_item.Quantity}", Theme.SmallFont, brush, priceRect, sf);
        }

        var totalRect = new Rectangle(rect.X + ContentPadding, rect.Y + 58, textWidth, 24);
        using (var brush = new SolidBrush(Theme.Accent))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            g.DrawString($"{_item.Total:N0} ₽", Theme.SectionTitleFont, brush, totalRect, sf);
        }

        DrawQtyButton(g, MinusRect, "\uE738", _qtyMinusPressed);
        DrawQtyButton(g, PlusRect, "\uE710", _qtyPlusPressed);
        DrawQtyButton(g, RemoveRect, "\uE711", _removePressed, Theme.Danger);

        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_item.Quantity.ToString(), Theme.SectionTitleFont, brush, QtyRect, sf);
        }
    }

    private void DrawQtyButton(Graphics g, Rectangle rect, string icon, bool pressed, Color? accentColor = null)
    {
        Color bgColor;
        if (pressed)
            bgColor = accentColor ?? Theme.AccentPressed;
        else if (_hover)
            bgColor = accentColor ?? Theme.SurfaceHover;
        else
            bgColor = Theme.SurfaceElevated;

        using (var path = Theme.RoundedRect(rect, 6))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }
            using (var pen = new Pen(Theme.Border, 1))
            {
                g.DrawPath(pen, path);
            }
        }

        Color iconColor = accentColor.HasValue && (pressed || _hover) ? Color.White : Theme.TextPrimary;

        using (var iconFont = new Font(Theme.IconFontFamily, 14))
        using (var brush = new SolidBrush(iconColor))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(icon, iconFont, brush, rect, sf);
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
        _qtyMinusPressed = _qtyPlusPressed = _removePressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        if (MinusRect.Contains(e.Location))
            _qtyMinusPressed = true;
        else if (PlusRect.Contains(e.Location))
            _qtyPlusPressed = true;
        else if (RemoveRect.Contains(e.Location))
            _removePressed = true;

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        if (_qtyMinusPressed && MinusRect.Contains(e.Location) && _item != null)
            QuantityChanged?.Invoke(this, Math.Max(1, _item.Quantity - 1));
        else if (_qtyPlusPressed && PlusRect.Contains(e.Location) && _item != null)
        {
            int next = _item.Quantity + 1;
            if (MaxQuantity > 0 && next > MaxQuantity)
                QuantityChanged?.Invoke(this, _item.Quantity); // лимит склада
            else
                QuantityChanged?.Invoke(this, next);
        }
        else if (_removePressed && RemoveRect.Contains(e.Location))
            RemoveClicked?.Invoke(this, EventArgs.Empty);

        _qtyMinusPressed = _qtyPlusPressed = _removePressed = false;
        Invalidate();
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}
