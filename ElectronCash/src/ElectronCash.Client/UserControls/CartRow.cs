using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Models;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Строка товара в корзине с количеством и суммой.
/// </summary>
public class CartRow : Control
{
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
        Height = 80;
        MinimumSize = new Size(400, 80);
        Font = Theme.RegularFont;
    }

    /// <summary>Элемент заказа для отображения.</summary>
    public OrderItem? Item
    {
        get => _item;
        set { _item = value; Invalidate(); }
    }

    /// <summary>Событие изменения количества.</summary>
    public event EventHandler<int>? QuantityChanged;

    /// <summary>Событие удаления товара.</summary>
    public event EventHandler? RemoveClicked;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Фон строки
        Color bgColor = _hover ? Theme.SurfaceHover : Theme.Surface;
        
        using (var path = Theme.RoundedRect(rect, 8))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }

            // Тонкая рамка
            using (var pen = new Pen(Theme.Border, 1))
            {
                g.DrawPath(pen, path);
            }
        }

        if (_item == null) return;

        // Название товара
        var nameRect = new Rectangle(rect.X + 16, rect.Y + 8, rect.Width - 200, 24);
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(_item.ProductName, Theme.RegularFont, brush, nameRect, sf);
        }

        // Цена за единицу
        var priceRect = new Rectangle(rect.X + 16, rect.Y + 36, 100, 20);
        using (var brush = new SolidBrush(Theme.TextSecondary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            g.DrawString($"{_item.UnitPrice:N0} ₽ × {_item.Quantity}", Theme.SmallFont, brush, priceRect, sf);
        }

        // Кнопка уменьшения количества
        var minusRect = new Rectangle(rect.X + rect.Width - 180, rect.Y + 20, 36, 36);
        DrawQtyButton(g, minusRect, "\uE70D", _qtyMinusPressed, _hover); // minus

        // Количество
        var qtyRect = new Rectangle(rect.X + rect.Width - 140, rect.Y + 20, 40, 36);
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_item.Quantity.ToString(), Theme.SectionTitleFont, brush, qtyRect, sf);
        }

        // Кнопка увеличения количества
        var plusRect = new Rectangle(rect.X + rect.Width - 96, rect.Y + 20, 36, 36);
        DrawQtyButton(g, plusRect, "\uE710", _qtyPlusPressed, _hover); // plus

        // Сумма
        var totalRect = new Rectangle(rect.X + rect.Width - 180, rect.Y + 8, 160, 24);
        using (var brush = new SolidBrush(Theme.Accent))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near };
            g.DrawString($"{_item.Total:N0} ₽", Theme.SectionTitleFont, brush, totalRect, sf);
        }

        // Кнопка удаления
        var removeRect = new Rectangle(rect.X + rect.Width - 48, rect.Y + 20, 36, 36);
        DrawQtyButton(g, removeRect, "\uE711", _removePressed, _hover, Theme.Danger); // close
    }

    private void DrawQtyButton(Graphics g, Rectangle rect, string icon, bool pressed, bool hover, Color? accentColor = null)
    {
        Color bgColor;
        if (pressed)
            bgColor = accentColor ?? Theme.AccentPressed;
        else if (hover)
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

        Color iconColor = accentColor.HasValue && (pressed || hover) ? Color.White : Theme.TextPrimary;
        if (accentColor.HasValue && accentColor.Value == Theme.Danger && (pressed || hover))
            iconColor = Color.White;

        using (var iconFont = new Font("Segoe MDL2 Assets", 14))
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
        if (e.Button == MouseButtons.Left)
        {
            var minusRect = new Rectangle(Width - 180, 20, 36, 36);
            var plusRect = new Rectangle(Width - 96, 20, 36, 36);
            var removeRect = new Rectangle(Width - 48, 20, 36, 36);

            if (minusRect.Contains(e.Location))
                _qtyMinusPressed = true;
            else if (plusRect.Contains(e.Location))
                _qtyPlusPressed = true;
            else if (removeRect.Contains(e.Location))
                _removePressed = true;

            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            var minusRect = new Rectangle(Width - 180, 20, 36, 36);
            var plusRect = new Rectangle(Width - 96, 20, 36, 36);
            var removeRect = new Rectangle(Width - 48, 20, 36, 36);

            if (_qtyMinusPressed && minusRect.Contains(e.Location) && _item != null)
            {
                QuantityChanged?.Invoke(this, Math.Max(1, _item.Quantity - 1));
            }
            else if (_qtyPlusPressed && plusRect.Contains(e.Location) && _item != null)
            {
                QuantityChanged?.Invoke(this, _item.Quantity + 1);
            }
            else if (_removePressed && removeRect.Contains(e.Location))
            {
                RemoveClicked?.Invoke(this, EventArgs.Empty);
            }

            _qtyMinusPressed = _qtyPlusPressed = _removePressed = false;
            Invalidate();
        }
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}