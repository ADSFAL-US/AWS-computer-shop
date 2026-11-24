using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Плитка товара. Все элементы позиционируются относительно текущего размера,
/// поэтому плитка корректно масштабируется вместе с сеткой.
/// Картинка подгружается из кеша/бэкенда в фоне, чтобы не тормозить прокрутку.
/// </summary>
public class ProductTile : Control
{
    private const int ContentPadding = 12;
    private const int ButtonHeight = 32;
    private const int NameHeight = 40;
    private const int PriceHeight = 26;

    private Product? _product;
    private bool _hover;
    private bool _pressed;
    private Image? _image;
    private bool _imageRequested;

    public ProductTile()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Size = new Size(220, 300);
        MinimumSize = new Size(150, 220);
        Cursor = Cursors.Hand;
        Font = Theme.RegularFont;
    }

    /// <summary>Товар для отображения.</summary>
    public Product? Product
    {
        get => _product;
        set
        {
            _product = value;
            _imageRequested = false;
            ReplaceImage(null);
            LoadImageIfNeeded();
            Invalidate();
        }
    }

    /// <summary>Событие клика по кнопке добавления.</summary>
    public event EventHandler? AddToCartClicked;

    private Rectangle ButtonRect
    {
        get
        {
            int width = Width - ContentPadding * 2;
            return new Rectangle(ContentPadding, Height - ContentPadding - ButtonHeight, width, ButtonHeight);
        }
    }

    /// <summary>Подтягивает картинку из кеша, а при промахе — с бэкенда.</summary>
    private async void LoadImageIfNeeded()
    {
        if (_product == null || !_product.HasImage || _imageRequested) return;
        _imageRequested = true;

        var product = _product;
        try
        {
            var cached = ProductImageCache.Instance.TryGetCached(product);
            if (cached != null)
            {
                if (ReferenceEquals(_product, product))
                {
                    ReplaceImage(cached);
                    Invalidate();
                }
                else
                {
                    cached.Dispose();
                }
                return;
            }

            var image = await ProductImageCache.Instance.GetAsync(product);
            if (image == null) return;
            if (IsDisposed || !ReferenceEquals(_product, product))
            {
                image.Dispose();
                return;
            }

            ReplaceImage(image);
            Invalidate();
        }
        catch (Exception ex)
        {
            AppLog.Error($"Ошибка загрузки изображения товара {product.Id}", ex);
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

        Color bgColor = _pressed ? Theme.SurfaceHover : _hover ? Theme.SurfaceElevated : Theme.Surface;

        using (var path = Theme.RoundedRect(rect, 12))
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

        if (_product == null) return;

        var buttonRect = ButtonRect;
        int contentWidth = rect.Width - ContentPadding * 2;

        // Снизу вверх: кнопка, цена, название; остальное — изображение
        var priceRect = new Rectangle(ContentPadding, buttonRect.Top - 6 - PriceHeight, contentWidth, PriceHeight);
        var nameRect = new Rectangle(ContentPadding, priceRect.Top - NameHeight, contentWidth, NameHeight);
        int imageHeight = Math.Max(48, nameRect.Top - ContentPadding - 8);
        var imageRect = new Rectangle(ContentPadding, ContentPadding, contentWidth, imageHeight);

        DrawImageArea(g, imageRect);

        // Название
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(_product.Name, Theme.ProductNameFont, brush, nameRect, sf);
        }

        // Цена
        using (var brush = new SolidBrush(Theme.Accent))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString($"{_product.Price:N0} ₽", Theme.SectionTitleFont, brush, priceRect, sf);
        }

        DrawStockBadge(g, priceRect);

        // Кнопка добавления
        bool available = _product.InStock;
        var btnPath = Theme.RoundedRect(buttonRect, 6);
        using (btnPath)
        {
            Color btnBg = !available
                ? Theme.SurfaceElevated
                : _pressed ? Theme.AccentPressed : _hover ? Theme.AccentHover : Theme.Accent;
            using var brush = new SolidBrush(btnBg);
            g.FillPath(brush, btnPath);
        }

        using (var brush = new SolidBrush(available ? Theme.Background : Theme.TextMuted))
        {
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(available ? "+ Корзина" : "Нет в наличии", Theme.RegularFont, brush, buttonRect, sf);
        }
    }

    private void DrawImageArea(Graphics g, Rectangle imageRect)
    {
        using var imagePath = Theme.RoundedRect(imageRect, 8);
        var previousClip = g.Clip;
        g.SetClip(imagePath);

        using (var bgBrush = new SolidBrush(Theme.Background))
        {
            g.FillPath(bgBrush, imagePath);
        }

        if (_image != null)
        {
            // Вписываем картинку с сохранением пропорций и обрезаем по плитке
            float scale = Math.Max(
                imageRect.Width / (float)_image.Width,
                imageRect.Height / (float)_image.Height);
            int drawWidth = (int)(_image.Width * scale);
            int drawHeight = (int)(_image.Height * scale);
            int x = imageRect.X + (imageRect.Width - drawWidth) / 2;
            int y = imageRect.Y + (imageRect.Height - drawHeight) / 2;
            g.DrawImage(_image, new Rectangle(x, y, drawWidth, drawHeight));
        }
        else
        {
            float iconSize = Math.Clamp(imageRect.Height * 0.34f, 24f, 56f);
            using var iconFont = new Font(Theme.IconFontFamily, iconSize);
            using var iconBrush = new SolidBrush(Theme.TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_product?.Icon ?? string.Empty, iconFont, iconBrush, imageRect, sf);
        }

        g.Clip = previousClip;
    }

    private void DrawStockBadge(Graphics g, Rectangle priceRect)
    {
        if (_product == null || _product.Stock > 5) return;

        string text = _product.InStock ? $"Осталось: {_product.Stock}" : "Нет в наличии";
        Color color = _product.InStock ? Theme.Warning : Theme.Danger;
        var size = TextRenderer.MeasureText(text, Theme.SmallFont);
        var badge = new Rectangle(
            priceRect.Right - size.Width - 2, priceRect.Top + 4, size.Width, size.Height);
        if (badge.Left <= priceRect.Left + 60) return;

        using var brush = new SolidBrush(color);
        g.DrawString(text, Theme.SmallFont, brush, badge.Location);
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
        if (e.Button == MouseButtons.Left && ButtonRect.Contains(e.Location) && _product?.InStock == true)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        if (_pressed && ButtonRect.Contains(e.Location))
            AddToCartClicked?.Invoke(this, EventArgs.Empty);

        _pressed = false;
        Invalidate();
    }

    /// <summary>Меняет картинку, освобождая предыдущую.</summary>
    private void ReplaceImage(Image? image)
    {
        if (ReferenceEquals(_image, image)) return;
        var old = _image;
        _image = image;
        old?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ReplaceImage(null);
        base.Dispose(disposing);
    }

    public void ApplyTheme()
    {
        Invalidate();
    }
}
