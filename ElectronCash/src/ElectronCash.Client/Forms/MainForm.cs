using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.UserControls;
using ElectronCash.Client.Theming;
namespace ElectronCash.Client.Forms;

/// <summary>
/// Главная форма. Раскладка построена на Dock/TableLayoutPanel, поэтому все
/// элементы тянутся за окном: шапка, чипы, сетка товаров и корзина.
/// </summary>
public class MainForm : ModernForm
{
    private const int CartWidth = 420;
    private const int TileGap = 16;
    private const int IdealTileWidth = 240;
    private const int MinTileWidth = 170;
    private const int MaxTileWidth = 340;
    private const int HeaderGap = 16;
    private const int MinSearchWidth = 140;
    private const int HeaderRowHeight = 56;
    private const int BadgeMinWidth = 1080;
    private const int CompactHeaderBelow = 1000;

    private readonly SearchBox _searchBox;
    private readonly FlatButton _btnCart;
    private readonly FlatButton _btnLogout;
    private readonly FlatButton? _btnAdmin;
    private readonly Panel _headerLayout;
    private readonly TableLayoutPanel _leftLayout;
    private readonly FlowLayoutPanel _headerButtons;
    private readonly Label _userBadge;
    private readonly ToolTip _headerTips = new() { InitialDelay = 400, ReshowDelay = 200 };
    private readonly FlowLayoutPanel _categoryChipsPanel;
    private readonly ScrollHost _productsScroll;
    private readonly FlowLayoutPanel _productsPanel;
    private readonly TableLayoutPanel _rootLayout;
    private readonly Panel _cartSidebar;
    private readonly ScrollHost _cartScroll;
    private readonly FlowLayoutPanel _cartItemsPanel;
    private readonly Label _cartTotalLabel;
    private readonly Label _cartEmptyLabel;
    private readonly FlatButton _btnCheckout;

    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private List<Category> _categories = new();
    private string _selectedCategory = "Все";
    private bool _cartVisible;
    private bool _compactHeader;
    private string? _loadError;
    private bool _loggingOut;

    public MainForm()
    {
        Text = "Электронная касса — Главная";
        Size = new Size(1200, 800);
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(0, 40, 0, 0);

        try
        {
            _categories = ApiClient.Instance.GetCategories();
            _allProducts = ApiClient.Instance.GetAllProducts();
        }
        catch (Exception ex)
        {
            _loadError = $"Не удалось загрузить каталог: {ex.Message}";
        }
        _filteredProducts = new List<Product>(_allProducts);

        // ---------- корневая сетка: контент + корзина ----------
        _rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0f));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // ---------- левая часть ----------
        _leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = new Padding(24, 16, 24, 16)
        };
        _leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // ---------- шапка ----------
        _headerLayout = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Height = HeaderRowHeight
        };

        _searchBox = new SearchBox("Поиск товаров...")
        {
            Location = new Point(0, 8),
            Height = 40,
            MinimumSize = new Size(120, 40)
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

        _headerButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _btnLogout = new FlatButton
        {
            Text = "Выход",
            Icon = "\uE74B",
            Variant = ButtonVariant.Secondary,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        _btnLogout.Click += BtnLogout_Click;

        if (Session.CurrentUser?.Role == Role.Admin)
        {
            _btnAdmin = new FlatButton
            {
                Text = "Админка",
                Icon = "\uE72C",
                Variant = ButtonVariant.Primary,
                AutoSize = true,
                Margin = new Padding(8, 0, 0, 0)
            };
            _btnAdmin.Click += async (s, e) =>
            {
                using (var adminForm = new AdminForm())
                    adminForm.ShowDialog(this);

                // В админке могли изменить цены, остатки и картинки
                await ReloadCatalogAsync();
            };
            _headerButtons.Controls.Add(_btnAdmin);
        }

        _btnCart = new FlatButton
        {
            Text = "Корзина (0)",
            Icon = "\uE7BF",
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        _btnCart.Click += BtnCart_Click;

        _userBadge = new Label
        {
            Text = Session.CurrentUser?.FullName ?? "Профиль",
            Font = Theme.RegularFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(0, 12, 0, 0)
        };

        _headerButtons.Controls.Add(_btnLogout);
        _headerButtons.Controls.Add(_btnCart);
        _headerButtons.Controls.Add(_userBadge);

        _headerTips = new ToolTip { InitialDelay = 400, ReshowDelay = 200 };
        _headerTips.SetToolTip(_btnLogout, "Выход");
        _headerTips.SetToolTip(_btnCart, "Корзина");
        if (_btnAdmin != null)
            _headerTips.SetToolTip(_btnAdmin, "Админка");

        _headerLayout.Controls.Add(_searchBox);
        _headerLayout.Controls.Add(_headerButtons);

        // Как в браузере: шапка всегда в одну строку. При сжатии кнопки
        // переходят в иконочный режим, подпись пользователя скрывается,
        // поиск забирает остаток ширины.
        void LayoutHeader()
        {
            int available = _headerLayout.ClientSize.Width;
            if (available <= 0) return;

            // Компактность считаем по ширине окна, а не по ширине шапки:
            // иначе открытие корзины внезапно сворачивает подписи кнопок.
            _compactHeader = ClientSize.Width < CompactHeaderBelow;
            _userBadge.Visible = !_compactHeader && ClientSize.Width >= BadgeMinWidth;
            _btnLogout.Text = _compactHeader ? string.Empty : "Выход";
            if (_btnAdmin != null)
                _btnAdmin.Text = _compactHeader ? string.Empty : "Админка";
            UpdateCartButtonText();

            int buttonsWidth = _headerButtons.PreferredSize.Width;
            _headerButtons.Location = new Point(
                Math.Max(0, available - buttonsWidth), 8);

            _searchBox.Width = Math.Max(MinSearchWidth, available - buttonsWidth - HeaderGap);
            _searchBox.Location = new Point(0, 8);
        }

        _headerLayout.Resize += (s, e) => LayoutHeader();
        _headerButtons.SizeChanged += (s, e) => LayoutHeader();
        LayoutHeader();

        // ---------- чипы категорий ----------
        _categoryChipsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 0, 8),
            Padding = Padding.Empty
        };
        CreateCategoryChips();

        // ---------- сетка товаров ----------
        _productsScroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        _productsScroll.Resize += (s, e) => RecalculateProductTileSizes();

        _productsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            AutoSize = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _productsPanel.Resize += (s, e) => RecalculateProductTileSizes();
        _productsScroll.ContentPanel.Controls.Add(_productsPanel);

        _leftLayout.Controls.Add(_headerLayout, 0, 0);
        _leftLayout.Controls.Add(_categoryChipsPanel, 0, 1);
        _leftLayout.Controls.Add(_productsScroll, 0, 2);

        // ---------- корзина ----------
        _cartSidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = Padding.Empty,
            Visible = false
        };
        _cartSidebar.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, 0, _cartSidebar.Height);
        };

        var cartLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
            Padding = new Padding(16, 12, 16, 16)
        };
        cartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        cartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        cartLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var cartHeader = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };

        var cartTitle = new Label
        {
            Text = "Корзина",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8)
        };

        var btnCloseCart = new FlatButton
        {
            Text = "",
            Icon = "\uE711",
            Variant = ButtonVariant.Ghost,
            Size = new Size(36, 36)
        };
        // Кнопка позиционируется вручную: Anchor вместе с Location даёт сдвиг,
        // из-за которого она уезжала за край панели.
        void LayoutCartClose()
        {
            btnCloseCart.Location = new Point(
                Math.Max(0, cartHeader.ClientSize.Width - btnCloseCart.Width), 4);
        }
        cartHeader.Resize += (s, e) => LayoutCartClose();
        LayoutCartClose();
        btnCloseCart.Click += (s, e) => SetCartVisible(false);

        cartHeader.Controls.Add(cartTitle);
        cartHeader.Controls.Add(btnCloseCart);

        _cartScroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 8, 0, 8)
        };
        _cartScroll.Resize += (s, e) => ResizeCartRows();

        _cartItemsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            AutoSize = false,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _cartItemsPanel.Resize += (s, e) => ResizeCartRows();
        _cartScroll.ContentPanel.Controls.Add(_cartItemsPanel);

        var cartFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };
        cartFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cartFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cartFooter.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _cartTotalLabel = new Label
        {
            Text = "Итого: 0 ₽",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.Accent,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _btnCheckout = new FlatButton
        {
            Text = "Оформить заказ",
            Icon = "\uE73E",
            Variant = ButtonVariant.Primary,
            Dock = DockStyle.Fill,
            Height = 48,
            Font = Theme.SectionTitleFont,
            Margin = Padding.Empty
        };
        _btnCheckout.Click += BtnCheckout_Click;

        _cartEmptyLabel = new Label
        {
            Text = "Корзина пуста\r\nДобавьте товары из каталога",
            Font = Theme.RegularFont,
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Visible = false
        };

        // Заглушка лежит в той же ячейке, что и список: показываем одно из двух
        var cartBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 8, 0, 8)
        };
        cartBody.Controls.Add(_cartScroll);
        cartBody.Controls.Add(_cartEmptyLabel);

        cartFooter.Controls.Add(_cartTotalLabel, 0, 0);
        cartFooter.Controls.Add(_btnCheckout, 0, 1);

        cartLayout.Controls.Add(cartHeader, 0, 0);
        cartLayout.Controls.Add(cartBody, 0, 1);
        cartLayout.Controls.Add(cartFooter, 0, 2);
        _cartSidebar.Controls.Add(cartLayout);

        _rootLayout.Controls.Add(_leftLayout, 0, 0);
        _rootLayout.Controls.Add(_cartSidebar, 1, 0);
        Controls.Add(_rootLayout);

        Session.CartChanged += Session_CartChanged;
        UpdateCartUI();
        RenderProducts();

        // Кеш картинок синхронизируем с тем, что реально отображается
        ProductImageCache.Instance.Sweep(_allProducts);
    }

    // ============= КАТАЛОГ =============

    private void CreateCategoryChips()
    {
        _categoryChipsPanel.Controls.Clear();

        var categories = new[] { "Все" }.Concat(
            _allProducts
                .Select(p => _categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(c => c)
        ).ToArray();

        foreach (var category in categories)
        {
            var chip = new ChipControl
            {
                ChipText = category!,
                Selected = category == _selectedCategory,
                BackgroundColor = Theme.SurfaceElevated,
                TextColor = Theme.TextPrimary,
                Margin = new Padding(0, 0, 8, 8)
            };
            chip.Click += (s, e) => CategoryChip_Click(chip, category!);
            _categoryChipsPanel.Controls.Add(chip);
        }
    }

    private void CategoryChip_Click(ChipControl clickedChip, string category)
    {
        _selectedCategory = category;

        foreach (Control ctrl in _categoryChipsPanel.Controls)
        {
            if (ctrl is ChipControl chip)
                chip.Selected = chip == clickedChip;
        }

        FilterProducts();
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e) => FilterProducts();

    private void FilterProducts()
    {
        var searchText = (_searchBox.Text ?? string.Empty).ToLower().Trim();

        _filteredProducts = _allProducts.Where(p =>
        {
            var categoryName = _categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? string.Empty;
            return (string.IsNullOrEmpty(searchText)
                    || p.Name.ToLower().Contains(searchText)
                    || categoryName.ToLower().Contains(searchText))
                   && (_selectedCategory == "Все" || categoryName == _selectedCategory);
        }).ToList();

        RenderProducts();
    }

    private void RenderProducts()
    {
        _productsPanel.SuspendLayout();
        _productsPanel.Controls.Clear();

        foreach (var product in _filteredProducts)
        {
            var tile = new ProductTile
            {
                Product = product,
                Margin = new Padding(0, 0, TileGap, TileGap)
            };
            tile.AddToCartClicked += (s, e) => AddToCart(product);
            _productsPanel.Controls.Add(tile);
        }

        _productsPanel.ResumeLayout();
        RecalculateProductTileSizes();
        _productsScroll.ScrollToTop();
    }

    /// <summary>
    /// Подбирает ширину плитки так, чтобы ряд заполнял доступную ширину без дыр.
    /// </summary>
    private void RecalculateProductTileSizes()
    {
        if (_productsPanel.Controls.Count == 0)
        {
            _productsScroll.UpdateScrollRange();
            return;
        }

        int available = _productsScroll.ContentWidth;
        if (available < MinTileWidth) available = MinTileWidth;

        // У каждой плитки есть правый отступ TileGap, он тоже входит в ширину ряда.
        // Округляем число колонок к ближайшему, иначе при ширине чуть меньше двух
        // идеальных плиток остаётся одна растянутая и полряда пустоты.
        int columns = Math.Max(1, (int)Math.Round(
            (available + TileGap) / (double)(IdealTileWidth + TileGap),
            MidpointRounding.AwayFromZero));

        int MeasureTileWidth(int cols) => (available - cols * TileGap) / cols;

        int tileWidth = MeasureTileWidth(columns);
        while (tileWidth > MaxTileWidth)
        {
            columns++;
            tileWidth = MeasureTileWidth(columns);
        }
        while (tileWidth < MinTileWidth && columns > 1)
        {
            columns--;
            tileWidth = MeasureTileWidth(columns);
        }
        tileWidth = Math.Clamp(tileWidth, MinTileWidth, MaxTileWidth);

        int tileHeight = Math.Clamp((int)(tileWidth * 1.3), 250, 420);

        _productsPanel.SuspendLayout();
        _productsPanel.Width = available;
        foreach (Control ctrl in _productsPanel.Controls)
        {
            if (ctrl is ProductTile tile)
                tile.Size = new Size(tileWidth, tileHeight);
        }

        int rows = (_productsPanel.Controls.Count + columns - 1) / columns;
        _productsPanel.Height = rows * (tileHeight + TileGap);
        _productsPanel.ResumeLayout();

        _productsScroll.UpdateScrollRange();
    }

    /// <summary>
    /// Перечитывает каталог с бэкенда (после оформления заказа или из админки)
    /// и синхронизирует кеш изображений с тем, что видно в UI.
    /// </summary>
    private async Task ReloadCatalogAsync()
    {
        try
        {
            var categories = await Task.Run(() => ApiClient.Instance.GetCategories());
            var products = await Task.Run(() => ApiClient.Instance.GetAllProducts());

            _categories = categories;
            _allProducts = products;
            _filteredProducts = new List<Product>(_allProducts);

            Session.PruneCart(_allProducts);
            CreateCategoryChips();
            FilterProducts();
            UpdateCartUI();

            ProductImageCache.Instance.Sweep(_allProducts);
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Не удалось обновить каталог: {ex.Message}", ToastType.Error);
        }
    }

    private void AddToCart(Product product)    {
        if (!product.InStock)
        {
            Toast.Show(this, $"«{product.Name}» нет в наличии", ToastType.Warning);
            return;
        }

        var existingItem = Session.CurrentCart.FirstOrDefault(i => i.ProductId == product.Id);
        int current = existingItem?.Quantity ?? 0;
        if (current >= product.Stock)
        {
            Toast.Show(this, $"На складе только {product.Stock} шт. «{product.Name}»", ToastType.Warning);
            return;
        }

        if (existingItem != null)
            existingItem.Quantity++;
        else
            Session.CurrentCart.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });

        Session.RaiseCartChanged();
        Toast.Show(this, $"{product.Name} добавлен в корзину", ToastType.Success);
    }

    // ============= КОРЗИНА =============

    private void Session_CartChanged() => UpdateCartUI();

    private void UpdateCartButtonText()
    {
        int itemCount = Session.CurrentCart.Sum(i => i.Quantity);
        _btnCart.Text = _compactHeader ? string.Empty : $"Корзина ({itemCount})";
    }

    private void UpdateCartUI()
    {
        int itemCount = Session.CurrentCart.Sum(i => i.Quantity);
        decimal total = Session.CurrentCart.Sum(i => i.Total);

        UpdateCartButtonText();

        _cartItemsPanel.SuspendLayout();
        _cartItemsPanel.Controls.Clear();
        foreach (var item in Session.CurrentCart)
        {
            var stock = _allProducts.FirstOrDefault(p => p.Id == item.ProductId)?.Stock ?? 0;
            var row = new CartRow
            {
                Item = item,
                MaxQuantity = stock,
                Margin = new Padding(0, 0, 0, 8)
            };
            row.QuantityChanged += (s, qty) =>
            {
                if (stock > 0 && qty > stock)
                {
                    Toast.Show(this, $"На складе только {stock} шт.", ToastType.Warning);
                    return;
                }
                if (qty == item.Quantity)
                    return;
                item.Quantity = qty;
                Session.RaiseCartChanged();
            };
            row.RemoveClicked += (s, e) =>
            {
                Session.CurrentCart.Remove(item);
                Session.RaiseCartChanged();
            };
            _cartItemsPanel.Controls.Add(row);
        }
        _cartItemsPanel.ResumeLayout();

        bool empty = Session.CurrentCart.Count == 0;
        _cartEmptyLabel.Visible = empty;
        _cartScroll.Visible = !empty;
        _btnCheckout.Enabled = !empty;

        ResizeCartRows();
        _cartScroll.UpdateScrollRange();
        _cartTotalLabel.Text = $"Итого: {total:N0} ₽";
    }

    private void ResizeCartRows()
    {
        _cartScroll.UpdateScrollRange();
        int width = Math.Max(160, _cartScroll.ContentWidth);

        _cartItemsPanel.SuspendLayout();
        _cartItemsPanel.Width = width;

        int height = 0;
        foreach (Control ctrl in _cartItemsPanel.Controls)
        {
            if (ctrl is CartRow row)
            {
                row.Width = width;
                height += row.Height + row.Margin.Vertical;
            }
        }
        _cartItemsPanel.Height = height;
        _cartItemsPanel.ResumeLayout();

        _cartScroll.UpdateScrollRange();
    }

    private void SetCartVisible(bool visible)
    {
        _cartVisible = visible;
        _cartSidebar.Visible = visible;
        _rootLayout.ColumnStyles[1].Width = visible ? GetCartWidth() : 0f;
        if (visible)
        {
            _cartSidebar.BringToFront();
            ResizeCartRows();
        }
    }

    /// <summary>Ширина корзины подстраивается под окно, чтобы не съедать каталог.</summary>
    private int GetCartWidth()
    {
        int available = ClientSize.Width - 48;
        return Math.Clamp((int)(available * 0.42), 300, CartWidth);
    }

    private void BtnCart_Click(object? sender, EventArgs e) => SetCartVisible(!_cartVisible);

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        if (Session.CurrentCart.Count == 0)
        {
            Toast.Show(this, "Корзина пуста", ToastType.Warning);
            return;
        }

        // Перед доп. предложениями сверяем остатки, чтобы не продать лишнего
        foreach (var item in Session.CurrentCart.ToList())
        {
            var product = _allProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null || !product.InStock)
            {
                Session.CurrentCart.Remove(item);
                Toast.Show(this, $"«{item.ProductName}» больше нет в наличии", ToastType.Warning);
                Session.RaiseCartChanged();
            }
            else if (item.Quantity > product.Stock)
            {
                item.Quantity = product.Stock;
                Toast.Show(this, $"«{product.Name}»: остаток {product.Stock} шт.", ToastType.Warning);
                Session.RaiseCartChanged();
            }
        }

        if (Session.CurrentCart.Count == 0)
            return;

        ShowCrossSellDialog();
    }

    // ============= КРОСС-ПРОДАЖИ =============

    private void ShowCrossSellDialog()
    {
        List<CrossSellOffer> offers;
        try
        {
            offers = ApiClient.Instance.GetCrossSellOffers();
        }
        catch (Exception ex)
        {
            // Предложения не критичны: без них заказ всё равно оформляем
            Toast.Show(this, $"Не удалось загрузить предложения: {ex.Message}", ToastType.Warning);
            ProcessCheckout();
            return;
        }

        using var dialog = new CrossSellDialog(offers, _allProducts, UpdateCartUI);
        dialog.ShowDialog(this);

        if (dialog.ProceedToCheckout && Session.CurrentCart.Count > 0)
            ProcessCheckout();
    }

    private async void ProcessCheckout()
    {
        _btnCheckout.Enabled = false;
        _btnCheckout.Text = "Обработка...";
        _btnCheckout.Loading = true;

        try
        {
            var items = Session.CurrentCart.ToList();
            var order = await ApiClient.Instance.CreateOrderAsync(items, Session.Discount);

            Session.CurrentCart.Clear();
            Session.RaiseCartChanged();

            SetCartVisible(false);
            Toast.Show(this, $"Заказ №{order.Id} успешно оформлен!", ToastType.Success);

            // Остатки изменились — перечитываем каталог и чистим кеш картинок
            // товаров, которых больше нет в наличии.
            await ReloadCatalogAsync();
        }
        catch (ApiException ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Ошибка оформления заказа: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _btnCheckout.Enabled = true;
            _btnCheckout.Text = "Оформить заказ";
            _btnCheckout.Loading = false;
        }
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        _loggingOut = true;
        ApiClient.Instance.Logout();
        Session.Logout();
        var loginForm = new LoginForm();
        loginForm.Show();
        Close();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_cartVisible)
            _rootLayout.ColumnStyles[1].Width = GetCartWidth();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_loadError != null)
            Toast.Show(this, _loadError, ToastType.Error);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Session.CartChanged -= Session_CartChanged;
        base.OnFormClosing(e);

        // LoginForm остаётся скрытым после входа, поэтому без этого процесс висит в фоне
        if (!_loggingOut)
            Application.Exit();
    }
}
