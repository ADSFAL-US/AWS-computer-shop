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
    private readonly ProgressBarCustom _progressBar;
    private readonly Label _statusLabel;
    
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

        // Чипы категорий
        _categoryChipsPanel = new FlowLayoutPanel
        {
            Location = new Point(24, 120),
            Size = new Size(Width - 48, 40),
            AutoSize = false,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
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
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 16, 0)
        };
        RenderProducts();

        // Боковая панель корзины (изначально скрыта)
        _cartSidebar = new Panel
        {
            Location = new Point(Width, 40),
            Size = new Size(360, Height - 40),
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
            Size = new Size(328, Height - 200),
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent
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
            Size = new Size(328, 48),
            Font = Theme.SectionTitleFont,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        _btnCheckout.Click += BtnCheckout_Click;

        _cartSidebar.Controls.AddRange(new Control[] { _cartTitle, _cartItemsPanel, _cartTotalLabel, _btnCheckout });

        // Прогресс-бар и статус
        _progressBar = new ProgressBarCustom
        {
            Location = new Point(24, Height - 40),
            Size = new Size(200, 8),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Visible = false
        };

        _statusLabel = new Label
        {
            Text = "Готово",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Location = new Point(240, Height - 44),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        Controls.AddRange(new Control[]
        {
            _searchBox, _btnCart, _btnProfile, _btnLogout,
            _categoryChipsPanel, _productsPanel,
            _cartSidebar, _progressBar, _statusLabel
        });

        // Обновляем корзину при изменении
        Session.CartChanged += Session_CartChanged;
        UpdateCartUI();

        // Обработка ресайза
        Resize += MainForm_Resize;
    }

    private void CreateCategoryChips()
    {
        _categoryChipsPanel.Controls.Clear();
        
        var categories = new[] { "Все" }.Concat(_allProducts.Select(p => p.Icon).Distinct().OrderBy(c => c)).ToArray();
        
        foreach (var category in categories)
        {
            var chip = new ChipControl
            {
                ChipText = category,
                Selected = category == _selectedCategory,
                BackgroundColor = Theme.SurfaceElevated,
                TextColor = Theme.TextPrimary,
                Margin = new Padding(0, 0, 8, 0)
            };
            chip.Click += (s, e) => CategoryChip_Click(chip, category);
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
            (string.IsNullOrEmpty(searchText) || p.Name.ToLower().Contains(searchText) || p.Icon.ToLower().Contains(searchText)) &&
            (_selectedCategory == "Все" || p.Icon == _selectedCategory)
        ).ToList();
        
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
                Width = 328
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
            _cartSidebar.Location = new Point(Width - 360, 40);
            _cartSidebar.Size = new Size(360, Height - 40);
            _cartItemsPanel.Size = new Size(328, Height - 200);
            _cartTotalLabel.Location = new Point(16, Height - 120);
            _btnCheckout.Location = new Point(16, Height - 100);
            _cartSidebar.BringToFront();
        }
    }

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        if (Session.CurrentCart.Count == 0)
        {
            Toast.Show(this, "Корзина пуста", ToastType.Warning);
            return;
        }

        _progressBar.Visible = true;
        _progressBar.Indeterminate = true;
        _statusLabel.Text = "Оформление заказа...";
        _btnCheckout.Enabled = false;

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
                    
                    _progressBar.Visible = false;
                    _progressBar.Indeterminate = false;
                    _statusLabel.Text = "Заказ оформлен!";
                    _btnCheckout.Enabled = true;
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
            _cartSidebar.Location = new Point(Width - 360, 40);
            _cartSidebar.Size = new Size(360, Height - 40);
            _cartItemsPanel.Size = new Size(328, Height - 200);
            _cartTotalLabel.Location = new Point(16, Height - 120);
            _btnCheckout.Location = new Point(16, Height - 100);
        }
        
        _categoryChipsPanel.Size = new Size(Width - 48, 40);
        _productsPanel.Size = new Size(_cartVisible ? Width - 408 : Width - 48, Height - 240);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Session.CartChanged -= Session_CartChanged;
        base.OnFormClosing(e);
    }
}