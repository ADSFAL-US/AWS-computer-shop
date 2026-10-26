using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.UserControls;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Главная форма приложения (1200x800).
/// </summary>
public class MainForm : ModernForm
{
    private readonly SearchBox _searchBox;
    private readonly FlatButton _btnCart;
    private readonly FlatButton _btnProfile;
    private readonly FlatButton _btnLogout;
    private readonly FlowLayoutPanel _categoryChipsPanel;
    private readonly FlowLayoutPanel _productsPanel;
    private readonly Panel _cartSidebar;
    private readonly Label _cartTitle;
    private readonly FlowLayoutPanel _cartItemsPanel;
    private readonly Label _cartTotalLabel;
    private readonly FlatButton _btnCheckout;
    private List<Product> _allProducts = new();
    private List<Product> _filteredProducts = new();
    private string _selectedCategory = "Все";
    private bool _cartVisible = false;

    public MainForm()
    {
        Text = "Электронная касса — Главная";
        Size = new Size(1200, 800);
        MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(0, 40, 0, 0);

        // Загружаем товары
        _allProducts = MockDataService.Instance.GetAllProducts();
        _filteredProducts = new List<Product>(_allProducts);

        // Поиск
        _searchBox = new SearchBox("Поиск товаров...")
        {
            Location = new Point(24, 60),
            Size = new Size(400, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        _searchBox.TextChanged += SearchBox_TextChanged;

        // Кнопки в заголовке
        _btnCart = new FlatButton
        {
            Text = "Корзина (0)",
            Icon = "\uE7BF", // shopping cart
            Variant = ButtonVariant.Ghost,
            Location = new Point(Width - 320, 60),
            Size = new Size(140, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnCart.Click += BtnCart_Click;

        _btnProfile = new FlatButton
        {
            Text = Session.CurrentUser?.FullName ?? "Профиль",
            Icon = "\uE716", // person
            Variant = ButtonVariant.Ghost,
            Location = new Point(Width - 170, 60),
            Size = new Size(140, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _btnLogout = new FlatButton
        {
            Text = "Выход",
            Icon = "\uE74B", // sign out
            Variant = ButtonVariant.Secondary,
            Location = new Point(Width - 170, 60),
            Size = new Size(100, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _btnLogout.Click += BtnLogout_Click;

        // Admin button (only for admins)
        FlatButton? _btnAdmin = null;
        if (Session.CurrentUser?.Role == Role.Admin)
        {
            _btnAdmin = new FlatButton
            {
                Text = "Админка",
                Icon = "\uE72C", // admin
                Variant = ButtonVariant.Primary,
                Location = new Point(Width - 480, 60),
                Size = new Size(120, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnAdmin.Click += (s, e) => 
            {
                var adminForm = new AdminForm();
                adminForm.ShowDialog(this);
            };
        }

        // Чипы категорий
        _categoryChipsPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 120),
            Size = new Size(Width - 48, 40),
            AutoSize = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Background
        };
        CreateCategoryChips();

        // Панель товаров
        _productsPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 180),
            Size = new Size(Width - 48, Height - 240),
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Background,
            Padding = new Padding(0, 0, 16, 0)
        };
        RenderProducts();

        // Боковая панель корзины (изначально скрыта) - wider at 420px
        _cartSidebar = new Panel
        {
            Location = new Point(Width, 40),
                    Size = new Size(420, Height - 40),
            BackColor = Theme.Surface,
            Visible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
        };
        _cartSidebar.Paint += (s, e) =>
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 0, 0, _cartSidebar.Height);
            }
        };

        // Close button for cart sidebar
        var btnCloseCart = new FlatButton
        {
            Text = "",
            Icon = "\uE711", // close
            Variant = ButtonVariant.Ghost,
                    Location = new Point(360, 12),
                    Size = new Size(40, 40),
            Font = Theme.RegularFont,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnCloseCart.Click += (s, e) => 
        {
            _cartVisible = false;
            _cartSidebar.Visible = false;
        };

        _cartTitle = new Label
        {
            Text = "Корзина",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        _cartItemsPanel = new FlowLayoutPanel
        {
            Location = new Point(16, 56),
                    Size = new Size(388, Height - 200),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Theme.Surface
        };

        _cartTotalLabel = new Label
        {
            Text = "Итого: 0 ₽",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.Accent,
            AutoSize = true,
            Location = new Point(16, Height - 120),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        _btnCheckout = new FlatButton
        {
            Text = "Оформить заказ",
            Icon = "\uE73E",
            Variant = ButtonVariant.Primary,
            Location = new Point(16, Height - 100),
                    Size = new Size(388, 48),
            Font = Theme.SectionTitleFont,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnCheckout.Click += BtnCheckout_Click;

        _cartSidebar.Controls.AddRange(new Control[] { _cartTitle, _cartItemsPanel, _cartTotalLabel, _btnCheckout, btnCloseCart });

        var mainControls = new List<Control> { _searchBox, _btnCart, _btnProfile, _btnLogout, _categoryChipsPanel, _productsPanel, _cartSidebar };
        if (_btnAdmin != null) mainControls.Add(_btnAdmin);
        Controls.AddRange(mainControls.ToArray());

        // Обновляем корзину при изменении
        Session.CartChanged += Session_CartChanged;
        UpdateCartUI();

        // Обработка ресайза
        Resize += MainForm_Resize;
    }

    private void CreateCategoryChips()
    {
        _categoryChipsPanel.Controls.Clear();
        
        var categories = new[] { "Все" }.Concat(
            _allProducts
                .Select(p => MockDataService.Instance.GetCategories().FirstOrDefault(c => c.Id == p.CategoryId)?.Name)
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
                Margin = new Padding(0, 0, 8, 0)
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
            {
                chip.Selected = chip == clickedChip;
            }
        }
        
        FilterProducts();
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        FilterProducts();
    }

    private void FilterProducts()
    {
        var searchText = _searchBox.Text.ToLower().Trim();
        
        _filteredProducts = _allProducts.Where(p => 
        {
            var categoryName = MockDataService.Instance.GetCategories().FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? string.Empty;
            return (string.IsNullOrEmpty(searchText) || p.Name.ToLower().Contains(searchText) || categoryName.ToLower().Contains(searchText)) &&
                   (_selectedCategory == "Все" || categoryName == _selectedCategory);
        }).ToList();
        
        RenderProducts();
    }

    private void RenderProducts()
    {
        _productsPanel.Controls.Clear();
        
        foreach (var product in _filteredProducts)
        {
            var tile = new ProductTile
            {
                Product = product,
                Margin = new Padding(0, 0, 16, 16)
            };
            tile.AddToCartClicked += (s, e) => AddToCart(product);
            _productsPanel.Controls.Add(tile);
        }
        
            // Calculate optimal tile sizes after adding all products
            RecalculateProductTileSizes();
        }

    private void AddToCart(Product product)
    {
        var existingItem = Session.CurrentCart.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else
        {
            Session.CurrentCart.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }
        
        Session.RaiseCartChanged();
        Toast.Show(this, $"{product.Name} добавлен в корзину", ToastType.Success);
    }

    private void Session_CartChanged()
    {
        UpdateCartUI();
    }

    private void UpdateCartUI()
    {
        int itemCount = Session.CurrentCart.Sum(i => i.Quantity);
        decimal total = Session.CurrentCart.Sum(i => i.Total);
        
        _btnCart.Text = $"Корзина ({itemCount})";
        
        _cartItemsPanel.Controls.Clear();
        foreach (var item in Session.CurrentCart)
        {
            var row = new CartRow
            {
                Item = item,
                Margin = new Padding(0, 0, 0, 8),
                            Width = 388
            };
            row.QuantityChanged += (s, qty) => 
            {
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
        
        _cartTotalLabel.Text = $"Итого: {total:N0} ₽";
    }

    private void BtnCart_Click(object? sender, EventArgs e)
    {
        _cartVisible = !_cartVisible;
        _cartSidebar.Visible = _cartVisible;
        
        if (_cartVisible)
        {
                _cartSidebar.Location = new Point(Width - 420, 40);
                _cartSidebar.Size = new Size(420, Height - 40);
                _cartItemsPanel.Size = new Size(388, Height - 200);
            _cartTotalLabel.Location = new Point(16, Height - 120);
            _btnCheckout.Location = new Point(16, Height - 100);
            _cartSidebar.BringToFront();
        }
            // Products panel size doesn't change - cart overlays on top
        }

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        if (Session.CurrentCart.Count == 0)
        {
            Toast.Show(this, "Корзина пуста", ToastType.Warning);
            return;
        }

        // Show cross-sell dialog before checkout
        ShowCrossSellDialog();
    }

    private void ShowCrossSellDialog()
    {
        var offers = MockDataService.Instance.GetCrossSellOffers();
        var accessoryOffers = offers.Where(o => o.Category == "accessory").ToList();
        var serviceOffers = offers.Where(o => o.Category == "service").ToList();

        var dialog = new Form
        {
            Text = "Дополнительные предложения",
            Size = new Size(500, 500),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Theme.Background,
            Font = Theme.RegularFont
        };

        var mainPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16)
        };

        // Accessories section
        if (accessoryOffers.Count > 0)
        {
            var sectionLabel = new Label
            {
                Text = "Аксессуары",
                Font = Theme.SectionTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            mainPanel.Controls.Add(sectionLabel);

            foreach (var offer in accessoryOffers)
            {
                mainPanel.Controls.Add(CreateOfferControl(offer, true));
            }
        }

        // Services section
        if (serviceOffers.Count > 0)
        {
            var sectionLabel = new Label
            {
                Text = "Сервисы",
                Font = Theme.SectionTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 16, 0, 8)
            };
            mainPanel.Controls.Add(sectionLabel);

            foreach (var offer in serviceOffers)
            {
                mainPanel.Controls.Add(CreateOfferControl(offer, false));
            }
        }

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16),
            BackColor = Theme.Surface
        };

        var btnSkip = new FlatButton
        {
            Text = "Пропустить",
            Variant = ButtonVariant.Ghost,
            Size = new Size(120, 40),
            Font = Theme.RegularFont
        };
        btnSkip.Click += (s, e) => { dialog.DialogResult = DialogResult.Cancel; dialog.Close(); };

        var btnContinue = new FlatButton
        {
            Text = "Оформить заказ",
            Variant = ButtonVariant.Primary,
            Size = new Size(160, 40),
            Font = Theme.SectionTitleFont
        };
        btnContinue.Click += (s, e) => { dialog.DialogResult = DialogResult.OK; dialog.Close(); };

        buttonPanel.Controls.Add(btnContinue);
        buttonPanel.Controls.Add(btnSkip);

        dialog.Controls.Add(mainPanel);
        dialog.Controls.Add(buttonPanel);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ProcessCheckout();
        }
    }

    private Control CreateOfferControl(CrossSellOffer offer, bool isAccessory)
    {
        var panel = new Panel
        {
            Height = 60,
            Width = 440,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Theme.Surface,
            Padding = new Padding(12)
        };
        panel.Paint += (s, e) =>
        {
            using (var path = Theme.RoundedRect(panel.ClientRectangle, 8))
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawPath(pen, path);
            }
        };

        var chk = new CheckBox
        {
            Text = "",
            Location = new Point(8, 16),
            Size = new Size(24, 24),
            Tag = offer,
            BackColor = Color.Transparent
        };
        chk.Paint += (s, e) =>
        {
            var cb = (CheckBox)s;
            var rect = new Rectangle(0, 0, 20, 20);
            using (var path = Theme.RoundedRect(rect, 4))
            {
                using (var brush = new SolidBrush(cb.Checked ? Theme.Accent : Theme.Surface))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (var pen = new Pen(cb.Checked ? Theme.Accent : Theme.Border, 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
                if (cb.Checked)
                {
                    using (var pen = new Pen(Theme.Background, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        e.Graphics.DrawLine(pen, 4, 10, 8, 14);
                        e.Graphics.DrawLine(pen, 8, 14, 16, 6);
                    }
                }
            }
        };

        var iconLabel = new Label
        {
            Text = offer.Icon,
            Font = new Font(Theme.IconFontFamily, 24),
            ForeColor = Theme.Accent,
            Location = new Point(40, 8),
            Size = new Size(48, 48),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var nameLabel = new Label
        {
            Text = offer.Name,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            Location = new Point(96, 8),
            Size = new Size(250, 24),
            BackColor = Color.Transparent
        };

        var descLabel = new Label
        {
            Text = offer.Description,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(96, 32),
            Size = new Size(250, 20),
            BackColor = Color.Transparent
        };

        var priceLabel = new Label
        {
            Text = $"{offer.Price:N0} ₽",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.Accent,
            Location = new Point(360, 16),
            Size = new Size(80, 28),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };

        panel.Controls.AddRange(new Control[] { chk, iconLabel, nameLabel, descLabel, priceLabel });
        return panel;
    }

    private void ProcessCheckout()
    {
        _btnCheckout.Enabled = false;
        _btnCheckout.Text = "Обработка...";
        _btnCheckout.Loading = true;

        Task.Run(async () =>
        {
            await Task.Delay(2000); // Имитация обработки
            
            if (InvokeRequired)
            {
                Invoke(() =>
                {
                    var order = new Order
                    {
                        CashierId = Session.CurrentUser?.Id ?? 0,
                        Items = new List<OrderItem>(Session.CurrentCart),
                        Total = Session.CurrentCart.Sum(i => i.Total),
                        Status = OrderStatus.Paid,
                        CreatedAt = DateTime.Now
                    };
                    
                    MockDataService.Instance.AddOrder(order);
                    Session.CurrentCart.Clear();
                    Session.RaiseCartChanged();
                    
                    _btnCheckout.Enabled = true;
                    _btnCheckout.Text = "Оформить заказ";
                    _btnCheckout.Loading = false;
                    _cartVisible = false;
                    _cartSidebar.Visible = false;
                    
                    Toast.Show(this, $"Заказ №{order.Id} успешно оформлен!", ToastType.Success);
                });
            }
        });
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        Session.Logout();
        var loginForm = new LoginForm();
        loginForm.Show();
        Close();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (_cartVisible)
        {
                _cartSidebar.Location = new Point(Width - 420, 40);
                _cartSidebar.Size = new Size(420, Height - 40);
                _cartItemsPanel.Size = new Size(388, Height - 200);
            _cartTotalLabel.Location = new Point(16, Height - 120);
            _btnCheckout.Location = new Point(16, Height - 100);
                _cartSidebar.BringToFront();
            }
        
            _categoryChipsPanel.Size = new Size(Width - 48, 40);
            // Products panel always uses full width - cart overlays on top (never shifts)
            _productsPanel.Size = new Size(Width - 48, Height - 240);
        
            // Recalculate product tile sizes to fill available width optimally
            RecalculateProductTileSizes();
        }

        private void RecalculateProductTileSizes()
        {
            if (_productsPanel.Controls.Count == 0) return;
        
            // Available width for products (accounting for padding and scrollbar)
            int availableWidth = _productsPanel.ClientSize.Width - 16; // 16px right padding
            if (_productsPanel.VerticalScroll.Visible)
                availableWidth -= SystemInformation.VerticalScrollBarWidth;
        
            if (availableWidth < 200) availableWidth = 200;
        
            // Calculate optimal tile width to fill the row
            const int idealTileWidth = 220;
            const int minTileWidth = 180;
            const int maxTileWidth = 280;
            const int tileMargin = 16; // right margin between tiles
        
            // How many tiles can fit in a row
            int tilesPerRow = Math.Max(1, (availableWidth + tileMargin) / (idealTileWidth + tileMargin));
        
            // Calculate actual tile width to perfectly fill the row
            int tileWidth = (availableWidth - (tilesPerRow - 1) * tileMargin) / tilesPerRow;
            tileWidth = Math.Clamp(tileWidth, minTileWidth, maxTileWidth);
        
            // Calculate proportional height (maintain aspect ratio ~220:300 = 0.733)
            int tileHeight = (int)(tileWidth * 300.0 / 220.0);
            tileHeight = Math.Clamp(tileHeight, 260, 340);
        
            // Apply to all product tiles
            foreach (Control ctrl in _productsPanel.Controls)
            {
                if (ctrl is ProductTile tile)
                {
                    tile.Size = new Size(tileWidth, tileHeight);
                }
            }
        }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Session.CartChanged -= Session_CartChanged;
        base.OnFormClosing(e);
    }
}