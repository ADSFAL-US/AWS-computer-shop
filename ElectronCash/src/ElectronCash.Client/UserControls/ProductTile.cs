using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Models;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Плитка товара с изображением, названием, ценой и кнопкой добавления.
/// </summary>
public class ProductTile : Control
{
    private Product? _product;
    private bool _hover;
    private bool _pressed;

    public ProductTile()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Size = new Size(200, 280);
        MinimumSize = new Size(180, 260);
        Cursor = Cursors.Hand;
        Font = Theme.RegularFont;
    }

    /// <summary>Товар для отображения.</summary>
    public Product? Product
    {
        get => _product;
        set { _product = value; Invalidate(); }
    }

    /// <summary>Событие клика по кнопке добавления.</summary>
    public event EventHandler? AddToCartClicked;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Фон карточки
        Color bgColor = _pressed ? Theme.SurfaceHover : _hover ? Theme.SurfaceElevated : Theme.Surface;
        
        using (var path = Theme.RoundedRect(rect, 12))
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

        if (_product == null) return;

        // Область изображения (верхняя часть)
        var imageRect = new Rectangle(rect.X + 12, rect.Y + 12, rect.Width - 24, 140);
        using (var imagePath = Theme.RoundedRect(imageRect, 8))
        {
            // Рисуем placeholder для изображения
            using (var brush = new SolidBrush(Theme.Background))
            {
                g.FillPath(brush, imagePath);
            }
            
            // Иконка категории товара
            using (var iconFont = new Font(Theme.IconFontFamily, 48))
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_product.Icon, iconFont, brush, imageRect, sf);
            }
        }

        // Название товара
        var nameRect = new Rectangle(rect.X + 12, rect.Y + 160, rect.Width - 24, 40);
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(_product.Name, Theme.ProductNameFont, brush, nameRect, sf);
        }

        // Цена и кнопка добавления
        var priceRect = new Rectangle(rect.X + 12, rect.Y + 220, 100, 32);
        using (var brush = new SolidBrush(Theme.Accent))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            g.DrawString($"{_product.Price:N0} ₽", Theme.SectionTitleFont, brush, priceRect, sf);
        }

        // Кнопка добавления в корзину
        var btnRect = new Rectangle(rect.X + rect.Width - 100, rect.Y + 220, 80, 32);
        using (var btnPath = Theme.RoundedRect(btnRect, 6))
        {
            Color btnBg = _pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent;
            using (var brush = new SolidBrush(btnBg))
            {
                g.FillPath(brush, btnPath);
            }
        }

        using (var brush = new SolidBrush(Theme.Background))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("+ Корзина", Theme.RegularFont, brush, btnRect, sf);
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
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            var btnRect = new Rectangle(Width - 100, 220, 80, 32);
            if (btnRect.Contains(e.Location))
            {
                _pressed = true;
                Invalidate();
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            var btnRect = new Rectangle(Width - 100, 220, 80, 32);
            if (_pressed && btnRect.Contains(e.Location))
            {
                AddToCartClicked?.Invoke(this, EventArgs.Empty);
            }
            _pressed = false;
            Invalidate();
        }
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}