using System.Drawing;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;
using ElectronCash.Client.UserControls;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Диалог дополнительных предложений перед оформлением заказа.
/// Предложения привязаны к товарам на складе, поэтому их можно реально
/// положить в корзину и убрать обратно.
/// </summary>
internal sealed class CrossSellDialog : ModernForm
{
    private readonly IReadOnlyList<Product> _products;
    private readonly Action _onCartChanged;
    private readonly FlowLayoutPanel _offersPanel;
    private readonly List<CrossSellOffer> _offers = new();
    private readonly Dictionary<int, FlatButton> _toggleButtons = new();

    /// <summary>true, если заказ нужно оформлять (нажали «Пропустить» или «Оформить»).</summary>
    public bool ProceedToCheckout { get; private set; }

    public CrossSellDialog(
        IReadOnlyList<CrossSellOffer> offers,
        IReadOnlyList<Product> products,
        Action onCartChanged)
    {
        _products = products;
        _onCartChanged = onCartChanged;
        _offers.AddRange(offers);

        Text = "Дополнительные предложения";
        Size = new Size(560, 600);
        MinimumSize = new Size(460, 460);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        Padding = new Padding(0, 40, 0, 0);

        var scroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        _offersPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            AutoSize = false,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16),
            BackColor = Theme.Background
        };
        scroll.ContentPanel.Controls.Add(_offersPanel);

        void RelayoutOffers()
        {
            int width = scroll.ContentWidth;
            if (width <= 0) return;

            _offersPanel.Width = width;
            int inner = width - _offersPanel.Padding.Horizontal;
            foreach (Control ctrl in _offersPanel.Controls)
            {
                if (ctrl is Panel offerPanel)
                    offerPanel.Width = Math.Max(280, inner);
            }

            _offersPanel.Height = Math.Max(
                scroll.ClientSize.Height,
                _offersPanel.GetPreferredSize(new Size(width, 0)).Height);
            scroll.UpdateScrollRange();
        }

        scroll.Resize += (s, e) => RelayoutOffers();
        _offersPanel.ControlAdded += (s, e) => RelayoutOffers();

        void AddSection(string title, IEnumerable<CrossSellOffer> sectionOffers, bool topMargin)
        {
            var list = sectionOffers.ToList();
            if (list.Count == 0) return;

            _offersPanel.Controls.Add(new Label
            {
                Text = title,
                Font = Theme.SectionTitleFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, topMargin ? 16 : 0, 0, 8)
            });

            foreach (var offer in list)
                _offersPanel.Controls.Add(CreateOfferControl(offer));
        }

        AddSection("Аксессуары", offers.Where(o => o.Category == "accessory"), false);
        AddSection("Сервисы", offers.Where(o => o.Category == "service"), true);

        _offersPanel.Controls.Add(new Label
        {
            Text = "Нажмите «Добавить», чтобы включить предложение в заказ",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 0)
        });

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(16, 14, 16, 14),
            BackColor = Theme.Surface
        };

        var btnSkip = new FlatButton
        {
            Text = "Пропустить",
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnSkip.Click += (s, e) =>
        {
            ProceedToCheckout = true;
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var btnContinue = new FlatButton
        {
            Text = "Оформить заказ",
            Variant = ButtonVariant.Primary,
            AutoSize = true,
            Font = Theme.SectionTitleFont,
            Margin = Padding.Empty
        };
        btnContinue.Click += (s, e) =>
        {
            ProceedToCheckout = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        buttonPanel.Controls.Add(btnContinue);
        buttonPanel.Controls.Add(btnSkip);

        Controls.Add(scroll);
        Controls.Add(buttonPanel);

        RelayoutOffers();
    }

    private Control CreateOfferControl(CrossSellOffer offer)
    {
        var panel = new Panel
        {
            Height = 64,
            Width = 440,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Theme.Surface,
            Padding = new Padding(12)
        };
        panel.Paint += (s, e) =>
        {
            using var path = Theme.RoundedRect(panel.ClientRectangle, 8);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawPath(pen, path);
        };

        var iconLabel = new Label
        {
            Text = offer.Icon,
            Font = new Font(Theme.IconFontFamily, 22),
            ForeColor = Theme.Accent,
            Location = new Point(12, 8),
            Size = new Size(44, 48),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var nameLabel = new Label
        {
            Text = offer.Name,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            Location = new Point(64, 8),
            Height = 24,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        var descLabel = new Label
        {
            Text = offer.Description,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Location = new Point(64, 32),
            Height = 20,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        var priceLabel = new Label
        {
            Text = $"{offer.Price:N0} ₽",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.Accent,
            Size = new Size(100, 28),
            TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent
        };
        var btnToggle = new FlatButton
        {
            Text = "Добавить",
            Variant = ButtonVariant.Secondary,
            AutoSize = false,
            Size = new Size(112, 34),
            Padding = new Padding(8, 0, 8, 0),
            Margin = Padding.Empty
        };
        btnToggle.Click += (s, e) => ToggleOffer(offer);

        if (offer.ProductId is null)
        {
            btnToggle.Enabled = false;
            btnToggle.Text = "Недоступно";
        }

        void Layout()
        {
            int right = panel.ClientSize.Width - panel.Padding.Right;
            int buttonLeft = right - btnToggle.Width;
            btnToggle.Location = new Point(buttonLeft, (panel.Height - btnToggle.Height) / 2);

            priceLabel.Location = new Point(buttonLeft - priceLabel.Width - 8, 18);
            int textWidth = Math.Max(60, priceLabel.Left - 8 - 64);
            nameLabel.Width = textWidth;
            descLabel.Width = textWidth;
        }

        panel.Resize += (s, e) => Layout();
        panel.Controls.AddRange(new Control[] { iconLabel, nameLabel, descLabel, priceLabel, btnToggle });
        Layout();

        _toggleButtons[offer.Id] = btnToggle;
        UpdateToggleState(offer);

        return panel;
    }

    private Product? FindProduct(int? productId) =>
        productId is int id ? _products.FirstOrDefault(p => p.Id == id) : null;

    private void ToggleOffer(CrossSellOffer offer)
    {
        if (offer.ProductId is not int productId) return;

        var existing = Session.CurrentCart.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
        {
            Session.CurrentCart.Remove(existing);
        }
        else
        {
            var product = FindProduct(productId);
            if (product is null || !product.InStock)
            {
                Toast.Show(this, $"«{offer.Name}» нет в наличии", ToastType.Warning);
                return;
            }

            Session.CurrentCart.Add(new OrderItem
            {
                ProductId = productId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }

        Session.RaiseCartChanged();
        _onCartChanged();

        // Обновляем подписи всех кнопок: состояние корзины общее
        foreach (var other in _toggleButtons)
            UpdateToggleButton(other.Key, other.Value);
    }

    private void UpdateToggleState(CrossSellOffer offer)
    {
        if (_toggleButtons.TryGetValue(offer.Id, out var button))
            UpdateToggleButton(offer.Id, button);
    }

    private void UpdateToggleButton(int offerId, FlatButton button)
    {
        var offer = _offers.FirstOrDefault(o => o.Id == offerId);
        if (offer?.ProductId is null)
            return;

        bool inCart = Session.CurrentCart.Any(i => i.ProductId == offer.ProductId);
        button.Text = inCart ? "Убрать" : "Добавить";
        button.Variant = inCart ? ButtonVariant.Primary : ButtonVariant.Secondary;
    }
}
