using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;
using ElectronCash.Client.UserControls;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Диалог создания/редактирования товара. Позволяет менять цену, остаток,
/// категорию, флаги доп. предложения и изображение (файл или ссылка).
/// </summary>
internal sealed class ProductEditDialog : ModernForm
{
    private readonly InputField _nameField;
    private readonly InputField _skuField;
    private readonly InputField _barcodeField;
    private readonly InputField _priceField;
    private readonly InputField _stockField;
    private readonly InputField _descriptionField;
    private readonly InputField _tagsField;
    private readonly DarkComboBox _categoryCombo;
    private readonly DarkComboBox _crossSellCategoryCombo;
    private readonly CheckBox _crossSellCheck;
    private readonly ImagePreview _preview;
    private readonly Label _statusLabel;

    private readonly Product _product;
    private readonly bool _isNew;
    private readonly List<Category> _categories;

    private byte[]? _pendingImage;
    private string? _pendingImageUrl;
    private bool _removeImage;
    private bool _saved;

    public Product Result => _product;

    public ProductEditDialog(Product? product, List<Category> categories)
    {
        _isNew = product is null;
        _product = product ?? new Product { CrossSellCategory = "accessory" };
        _categories = categories;

        Text = _isNew ? "Новый товар" : "Редактирование товара";
        Size = new Size(640, 720);
        MinimumSize = new Size(560, 640);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        Padding = new Padding(0, 40, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Padding = new Padding(20, 12, 20, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // ---------- левая колонка: изображение ----------
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Background,
            Margin = new Padding(0, 0, 16, 0)
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _preview = new ImagePreview
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        _statusLabel = new Label
        {
            Text = string.Empty,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            Dock = DockStyle.Bottom,
            Height = 22,
            Margin = new Padding(0, 6, 0, 0)
        };

        var btnPickFile = new FlatButton
        {
            Text = "Выбрать файл",
            Icon = "\uE8E5",
            Variant = ButtonVariant.Secondary,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };
        btnPickFile.Click += (s, e) => PickLocalFile();

        var btnPickUrl = new FlatButton
        {
            Text = "Загрузить по ссылке",
            Icon = "\uE71B",
            Variant = ButtonVariant.Secondary,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };
        btnPickUrl.Click += (s, e) => PickFromUrl();

        var btnRemoveImage = new FlatButton
        {
            Text = "Удалить изображение",
            Icon = "\uE74D",
            Variant = ButtonVariant.Ghost,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 6)
        };
        btnRemoveImage.Click += (s, e) =>
        {
            _pendingImage = null;
            _pendingImageUrl = null;
            _removeImage = true;
            _preview.Image = null;
            _statusLabel.Text = _isNew
                ? "Изображение будет не задано"
                : "Изображение будет удалено при сохранении";
        };

        left.Controls.Add(_preview, 0, 0);
        left.Controls.Add(btnPickFile, 0, 1);
        left.Controls.Add(btnPickUrl, 0, 2);
        left.Controls.Add(btnRemoveImage, 0, 3);
        left.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background }, 0, 4);

        // ---------- правая колонка: поля ----------
        var rightScroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        var right = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 20,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rightScroll.ContentPanel.Controls.Add(right);

        void AddCaption(string text)
        {
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            right.Controls.Add(new Label
            {
                Text = text,
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Margin = Padding.Empty
            });
            ResizeFields();
        }

        void AddField(Control field)
        {
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            right.Controls.Add(field);
            ResizeFields();
        }

        _nameField = new InputField("Название", "\uE7C3") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _skuField = new InputField("Артикул (можно оставить пустым)", "\uE8EC") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _barcodeField = new InputField("Штрихкод (можно оставить пустым)", "\uE8EC") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _descriptionField = new InputField("Описание", "\uE70B") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _tagsField = new InputField("Теги через запятую", "\uE8EC") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _priceField = new InputField("Цена, ₽", "\uE8C7") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _stockField = new InputField("Остаток на складе", "\uE7B8") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };

        _categoryCombo = new DarkComboBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
        _crossSellCategoryCombo = new DarkComboBox { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };

        _crossSellCheck = new CheckBox
        {
            Text = "Дополнительное предложение в кассе",
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0)
        };

        _nameField.Text = _product.Name;
        _skuField.Text = _product.Sku;
        _barcodeField.Text = _product.Barcode;
        _descriptionField.Text = _product.Description;
        _tagsField.Text = string.Join(", ", _product.Tags);
        _priceField.Text = _product.Price.ToString("0.##");
        _stockField.Text = _product.Stock.ToString();
        _crossSellCheck.Checked = _product.IsCrossSell;

        _categoryCombo.SetItems(_categories.Select(c => c.Name));
        var currentCategory = _categories.FirstOrDefault(c => c.Id == _product.CategoryId);
        if (currentCategory != null)
            _categoryCombo.SelectedItem = currentCategory.Name;

        var crossSellCategories = new Dictionary<string, string>
        {
            ["accessory"] = "Аксессуар",
            ["service"] = "Услуга"
        };
        _crossSellCategoryCombo.SetItems(crossSellCategories.Values);
        _crossSellCategoryCombo.SelectedItem =
            _product.CrossSellCategory == "service" ? "Услуга" : "Аксессуар";
        _crossSellCategoryCombo.Enabled = _crossSellCheck.Checked;
        _crossSellCheck.CheckedChanged += (s, e) =>
            _crossSellCategoryCombo.Enabled = _crossSellCheck.Checked;

        AddCaption("Название");
        AddField(_nameField);
        AddCaption("Артикул");
        AddField(_skuField);
        AddCaption("Штрихкод");
        AddField(_barcodeField);
        AddCaption("Категория");
        AddField(_categoryCombo);
        AddCaption("Цена");
        AddField(_priceField);
        AddCaption("Остаток");
        AddField(_stockField);
        AddCaption("Описание");
        AddField(_descriptionField);
        AddCaption("Теги");
        AddField(_tagsField);
        AddCaption("Дополнительное предложение");
        AddField(_crossSellCheck);
        AddField(_crossSellCategoryCombo);

        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(rightScroll, 1, 0);

        void ResizeFields()
        {
            int width = rightScroll.ContentWidth;
            if (width <= 0) return;
            right.Width = width;
            right.Height = right.GetPreferredSize(new Size(width, 0)).Height;
            rightScroll.UpdateScrollRange();
        }
        rightScroll.Resize += (s, e) => ResizeFields();
        right.ControlAdded += (s, e) => ResizeFields();
        ResizeFields();

        // ---------- кнопки ----------
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(20, 14, 20, 14)
        };

        var btnSave = new FlatButton
        {
            Text = "Сохранить",
            Icon = "\uE74E",
            Variant = ButtonVariant.Primary,
            AutoSize = true,
            Margin = Padding.Empty
        };
        btnSave.Click += (s, e) => Save();

        var btnCancel = new FlatButton
        {
            Text = "Отмена",
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            BackColor = Theme.Background,
            Padding = new Padding(20, 0, 20, 0)
        };
        footer.Controls.Add(_statusLabel);

        Controls.Add(layout);
        Controls.Add(footer);
        Controls.Add(btnPanel);

        // Показываем текущую картинку из кеша/бэкенда, если она есть
        LoadExistingImageAsync();
    }

    private async void LoadExistingImageAsync()
    {
        if (_isNew || !_product.HasImage) return;
        try
        {
            var image = await ProductImageCache.Instance.GetAsync(_product);
            if (image != null && !IsDisposed && _preview.Image == null)
                _preview.Image = image;
        }
        catch (Exception ex)
        {
            AppLog.Error("Не удалось показать изображение товара", ex);
        }
    }

    private void PickLocalFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите изображение товара",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp|Все файлы|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var bytes = File.ReadAllBytes(dialog.FileName);
            _pendingImage = bytes;
            _pendingImageUrl = null;
            _removeImage = false;
            _preview.Image = LoadPreview(bytes);
            _statusLabel.Text = $"Выбран файл: {Path.GetFileName(dialog.FileName)} ({bytes.Length / 1024} КБ)";
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Не удалось прочитать файл: {ex.Message}", ToastType.Error);
        }
    }

    private void PickFromUrl()
    {
        using var dialog = new TextInputDialog(
            "Изображение по ссылке",
            "Вставьте прямую ссылку на изображение (http/https)",
            "https://");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var url = dialog.Value.Trim();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Toast.Show(this, "Ссылка должна начинаться с http:// или https://", ToastType.Warning);
            return;
        }

        _pendingImageUrl = url;
        _pendingImage = null;
        _removeImage = false;
        _statusLabel.Text = "Изображение будет скачано при сохранении";

        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            _preview.Image = LoadPreview(bytes);
        }
        catch
        {
            // Предпросмотр не критичен — бэкенд скачает при сохранении
        }
    }

    private static Image? LoadPreview(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes);
            using var original = Image.FromStream(stream);
            return new Bitmap(original);
        }
        catch
        {
            return null;
        }
    }

    private static string ToDataUri(byte[] bytes, string filename)
    {
        string ext = Path.GetExtension(filename).ToLowerInvariant();
        string mime = ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "image/jpeg"
        };
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private void Save()
    {
        if (_saved) return;

        var name = _nameField.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Toast.Show(this, "Укажите название товара", ToastType.Warning);
            return;
        }
        if (!decimal.TryParse(_priceField.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price) || price < 0)
        {
            Toast.Show(this, "Некорректная цена", ToastType.Warning);
            return;
        }
        if (!int.TryParse(_stockField.Text.Trim(), out var stock) || stock < 0)
        {
            Toast.Show(this, "Некорректный остаток", ToastType.Warning);
            return;
        }

        var categoryName = _categoryCombo.SelectedItem?.ToString();
        var category = _categories.FirstOrDefault(c => c.Name == categoryName);
        if (category is null)
        {
            Toast.Show(this, "Выберите категорию", ToastType.Warning);
            return;
        }

        _product.Name = name;
        _product.Sku = _skuField.Text.Trim();
        _product.Barcode = _barcodeField.Text.Trim();
        _product.Description = _descriptionField.Text.Trim();
        _product.Tags = _tagsField.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _product.CategoryId = category.Id;
        _product.Price = price;
        _product.Stock = stock;
        _product.IsCrossSell = _crossSellCheck.Checked;
        _product.CrossSellCategory =
            _crossSellCategoryCombo.SelectedItem?.ToString() == "Услуга" ? "service" : "accessory";

        try
        {
            var saved = _isNew
                ? ApiClient.Instance.AddProduct(_product)
                : ApiClient.Instance.UpdateProduct(_product);

            _product.Id = saved.Id;
            _product.Sku = saved.Sku;
            _product.Barcode = saved.Barcode;
            _product.HasImage = saved.HasImage;
            _product.ImageToken = saved.ImageToken;

            ApplyImageChanges();

            // Перечитываем товар, чтобы получить актуальные флаг/версию картинки
            var refreshed = ApiClient.Instance.GetProductById(saved.Id);
            if (refreshed != null)
            {
                _product.HasImage = refreshed.HasImage;
                _product.ImageToken = refreshed.ImageToken;
                _product.ImageUpdatedAt = refreshed.ImageUpdatedAt;
            }

            _saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ApiException ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Не удалось сохранить товар: {ex.Message}", ToastType.Error);
        }
    }

    private void ApplyImageChanges()
    {
        if (_product.Id <= 0) return;

        if (_removeImage)
        {
            try
            {
                ApiClient.Instance.DeleteProductImage(_product.Id);
                _product.HasImage = false;
                _product.ImageToken = string.Empty;
            }
            catch (ApiException ex)
            {
                Toast.Show(this, $"Не удалось удалить изображение: {ex.Message}", ToastType.Error);
            }
            return;
        }

        string? payload = null;
        string filename = string.Empty;
        if (_pendingImage != null)
        {
            payload = ToDataUri(_pendingImage, "image.png");
        }
        else if (_pendingImageUrl != null)
        {
            payload = _pendingImageUrl;
        }

        if (payload is null) return;

        try
        {
            var meta = ApiClient.Instance.UploadProductImage(_product.Id, payload, filename);
            _product.HasImage = meta.Available;
            _product.ImageToken = meta.Token;
        }
        catch (ApiException ex)
        {
            Toast.Show(this, $"Товар сохранён, но изображение не загрузилось: {ex.Message}",
                ToastType.Warning);
        }
    }
}

/// <summary>Превью изображения с рамкой и заглушкой.</summary>
internal sealed class ImagePreview : Control
{
    private Image? _image;

    public ImagePreview()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Background;
    }

    public Image? Image
    {
        get => _image;
        set
        {
            if (ReferenceEquals(_image, value)) return;
            var old = _image;
            _image = value;
            old?.Dispose();
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Image = null;
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        using var path = Theme.RoundedRect(rect, 10);
        using (var brush = new SolidBrush(Theme.Surface))
            g.FillPath(brush, path);
        using (var pen = new Pen(Theme.Border))
            g.DrawPath(pen, path);

        if (_image == null)
        {
            using var iconFont = new Font(Theme.IconFontFamily, 34);
            using var iconBrush = new SolidBrush(Theme.TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("\uEB9F", iconFont, iconBrush, rect, sf);

            using var textBrush = new SolidBrush(Theme.TextMuted);
            g.DrawString("Нет изображения", Theme.SmallFont, textBrush,
                new Rectangle(rect.X, rect.Bottom - 26, rect.Width, 20),
                new StringFormat { Alignment = StringAlignment.Center });
            return;
        }

        var previous = g.Clip;
        g.SetClip(path);
        float scale = Math.Min(
            (rect.Width - 16) / (float)_image.Width,
            (rect.Height - 16) / (float)_image.Height);
        int w = (int)(_image.Width * scale);
        int h = (int)(_image.Height * scale);
        g.DrawImage(_image, new Rectangle(
            rect.X + (rect.Width - w) / 2, rect.Y + (rect.Height - h) / 2, w, h));
        g.Clip = previous;
    }
}

/// <summary>Простой диалог ввода одной строки.</summary>
internal sealed class TextInputDialog : ModernForm
{
    private readonly InputField _field;

    public string Value => _field.Text;

    public TextInputDialog(string title, string caption, string initial = "")
    {
        Text = title;
        Size = new Size(520, 260);
        MinimumSize = new Size(420, 240);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        AllowResize = false;
        Padding = new Padding(0, 40, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Padding = new Padding(20, 12, 20, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        layout.Controls.Add(new Label
        {
            Text = caption,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Margin = Padding.Empty
        }, 0, 0);

        _field = new InputField(string.Empty, "\uE71B") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _field.Text = initial;
        layout.Controls.Add(_field, 0, 1);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(20, 12, 20, 12)
        };

        var btnOk = new FlatButton
        {
            Text = "OK",
            Variant = ButtonVariant.Primary,
            AutoSize = true,
            Margin = Padding.Empty
        };
        btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new FlatButton
        {
            Text = "Отмена",
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnCancel);

        Controls.Add(layout);
        Controls.Add(btnPanel);
    }
}
