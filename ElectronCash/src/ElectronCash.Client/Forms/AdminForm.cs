using System.Drawing;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Административная панель (доступна только для Admin).
/// Вкладки — собственный сегментированный переключатель, чтобы не зависеть
/// от системной отрисовки TabControl (она давала белые полосы).
/// </summary>
public class AdminForm : ModernForm
{
    private readonly DataGridView _usersGrid;
    private readonly DataGridView _auditGrid;
    private readonly DataGridView _ipGrid;
    private readonly DataGridView _productsGrid;
    private readonly Panel _contentHost;
    private readonly Panel _usersPage;
    private readonly Panel _auditPage;
    private readonly Panel _ipPage;
    private readonly Panel _productsPage;
    private readonly List<(FlatButton Button, Panel Page)> _tabs = new();

    private List<Category> _categories = new();
    private string? _loadError;

    public AdminForm()
    {
        Text = "Административная панель";
        Size = new Size(1000, 700);
        MinimumSize = new Size(760, 520);
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(0, 40, 0, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Padding = new Padding(16, 12, 16, 12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

        // ---------- сегментированные вкладки ----------
        var tabStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };

        _usersPage = CreatePage();
        _auditPage = CreatePage();
        _ipPage = CreatePage();
        _productsPage = CreatePage();

        _usersGrid = CreateGrid();
        _usersGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Login", HeaderText = "Логин", DataPropertyName = "Login", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "ФИО", DataPropertyName = "FullName", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Роль", DataPropertyName = "Role", FillWeight = 25 }
        );

        _auditGrid = CreateGrid();
        _auditGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Время", DataPropertyName = "Timestamp", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "Пользователь", DataPropertyName = "UserName", FillWeight = 22 },
            new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Действие", DataPropertyName = "Action", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "Детали", DataPropertyName = "Details", FillWeight = 28 }
        );

        _ipGrid = CreateGrid();
        _ipGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "IpAddress", HeaderText = "IP-адрес", DataPropertyName = "IpAddress", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "Comment", HeaderText = "Комментарий", DataPropertyName = "Comment", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "AddedAt", HeaderText = "Добавлен", DataPropertyName = "AddedAt", FillWeight = 20 }
        );

        // ---------- страница «Пользователи» ----------
        var usersButtons = CreateButtonRow();
        var btnAddUser = CreateButton("Добавить", ButtonVariant.Primary);
        btnAddUser.Click += BtnAddUser_Click;
        var btnEditUser = CreateButton("Изменить", ButtonVariant.Secondary);
        btnEditUser.Click += BtnEditUser_Click;
        var btnDeleteUser = CreateButton("Удалить", ButtonVariant.Danger);
        btnDeleteUser.Click += BtnDeleteUser_Click;
        usersButtons.Controls.AddRange(new Control[] { btnAddUser, btnEditUser, btnDeleteUser });

        _usersPage.Controls.Add(_usersGrid);
        _usersPage.Controls.Add(usersButtons);

        // ---------- страница «Журнал аудита» ----------
        _auditPage.Controls.Add(_auditGrid);

        // ---------- страница «IP Whitelist» ----------
        var ipButtons = CreateButtonRow();
        var btnAddIp = CreateButton("Добавить IP", ButtonVariant.Primary);
        btnAddIp.Click += BtnAddIp_Click;
        var btnRemoveIp = CreateButton("Удалить", ButtonVariant.Danger);
        btnRemoveIp.Click += BtnRemoveIp_Click;
        ipButtons.Controls.AddRange(new Control[] { btnAddIp, btnRemoveIp });

        _ipPage.Controls.Add(_ipGrid);
        _ipPage.Controls.Add(ipButtons);

        // ---------- страница «Товары» ----------
        _productsGrid = CreateGrid();
        _productsGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 5 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Название", DataPropertyName = "Name", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "Артикул", DataPropertyName = "Sku", FillWeight = 13 },
            new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Категория", DataPropertyName = "Category", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Цена", DataPropertyName = "Price", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Склад", DataPropertyName = "Stock", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Image", HeaderText = "Фото", DataPropertyName = "Image", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "CrossSell", HeaderText = "Доп.", DataPropertyName = "CrossSell", FillWeight = 7 }
        );
        _productsGrid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) BtnEditProduct_Click(s, EventArgs.Empty);
        };

        var productButtons = CreateButtonRow();
        var btnAddProduct = CreateButton("Добавить", ButtonVariant.Primary);
        btnAddProduct.Click += BtnAddProduct_Click;
        var btnEditProduct = CreateButton("Изменить", ButtonVariant.Secondary);
        btnEditProduct.Click += BtnEditProduct_Click;
        var btnDeleteProduct = CreateButton("Удалить", ButtonVariant.Danger);
        btnDeleteProduct.Click += BtnDeleteProduct_Click;
        var btnImportProducts = CreateButton("Импорт", ButtonVariant.Secondary);
        btnImportProducts.Click += BtnImportProducts_Click;
        var btnAddCategory = CreateButton("Категория", ButtonVariant.Ghost);
        btnAddCategory.Click += BtnAddCategory_Click;
        productButtons.Controls.AddRange(
            new Control[] { btnAddProduct, btnEditProduct, btnDeleteProduct, btnImportProducts, btnAddCategory });

        _productsPage.Controls.Add(_productsGrid);
        _productsPage.Controls.Add(productButtons);

        _contentHost.Controls.Add(_usersPage);
        _contentHost.Controls.Add(_auditPage);
        _contentHost.Controls.Add(_ipPage);
        _contentHost.Controls.Add(_productsPage);

        AddTab(tabStrip, "Товары", _productsPage);
        AddTab(tabStrip, "Пользователи", _usersPage);
        AddTab(tabStrip, "Журнал аудита", _auditPage);
        AddTab(tabStrip, "IP Whitelist", _ipPage);

        // ---------- нижняя панель ----------
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        footer.Paint += (s, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var btnClose = new FlatButton
        {
            Text = "Закрыть",
            Variant = ButtonVariant.Secondary,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnClose.Click += (s, e) => Close();
        footer.Controls.Add(btnClose);
        footer.Resize += (s, e) => btnClose.Location = new Point(
            footer.Width - btnClose.Width, (footer.Height - btnClose.Height) / 2);

        root.Controls.Add(tabStrip, 0, 0);
        root.Controls.Add(_contentHost, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        SelectTab(_tabs[0].Button);
        LoadData();
        DarkScrollbars.ApplyToGrids(this);
    }

    private static Panel CreatePage() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.Background,
        Visible = false,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = Theme.Surface,
        BorderStyle = BorderStyle.None,
        GridColor = Theme.Border,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
        RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AllowUserToOrderColumns = false,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        RowHeadersVisible = false,
        EnableHeadersVisualStyles = false,
        Font = Theme.RegularFont,
        ColumnHeadersHeight = 40,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.SurfaceElevated,
            ForeColor = Theme.TextPrimary,
            Font = Theme.SectionTitleFont,
            SelectionBackColor = Theme.SurfaceElevated,
            SelectionForeColor = Theme.TextPrimary,
            Padding = new Padding(8, 0, 8, 0)
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            Font = Theme.RegularFont,
            SelectionBackColor = Theme.Accent,
            SelectionForeColor = Theme.Background,
            Padding = new Padding(8, 0, 8, 0)
        },
        RowTemplate = { Height = 40 }
    };

    private static FlowLayoutPanel CreateButtonRow() => new()
    {
        Dock = DockStyle.Bottom,
        Height = 56,
        FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false,
        BackColor = Theme.Background,
        Padding = new Padding(0, 12, 0, 0),
        Margin = Padding.Empty
    };

    private static FlatButton CreateButton(string text, ButtonVariant variant) => new()
    {
        Text = text,
        Variant = variant,
        AutoSize = true,
        Margin = new Padding(8, 0, 0, 0)
    };

    private void AddTab(FlowLayoutPanel strip, string title, Panel page)
    {
        var button = new FlatButton
        {
            Text = title,
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0)
        };
        button.Click += (s, e) => SelectTab(button);

        _tabs.Add((button, page));
        strip.Controls.Add(button);
    }

    private void SelectTab(FlatButton selected)
    {
        foreach (var (button, page) in _tabs)
        {
            bool isSelected = button == selected;
            button.Variant = isSelected ? ButtonVariant.Primary : ButtonVariant.Ghost;
            page.Visible = isSelected;
        }

        var current = _tabs.FirstOrDefault(t => t.Button == selected).Page;
        if (current != null)
            _contentHost.Controls.SetChildIndex(current, 0);
    }

    private void LoadData()
    {
        try
        {
            _categories = ApiClient.Instance.GetAdminCategories();

            _usersGrid.DataSource = ApiClient.Instance.GetUsers().Select(u => new
            {
                u.Id,
                u.Login,
                u.FullName,
                Role = u.Role.ToString()
            }).ToList();

            _auditGrid.DataSource = ApiClient.Instance.GetAuditLog().Select(a => new
            {
                a.Id,
                Timestamp = a.Timestamp.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                a.UserName,
                a.Action,
                a.Details
            }).ToList();

            _ipGrid.DataSource = ApiClient.Instance.GetIpWhitelist().Select(i => new
            {
                i.Id,
                i.IpAddress,
                i.Comment,
                AddedAt = i.AddedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            LoadProducts();
        }
        catch (Exception ex)
        {
            _loadError = $"Не удалось загрузить данные: {ex.Message}";
        }
    }

    private void LoadProducts()
    {
        var products = ApiClient.Instance.GetAdminProducts();
        _productsGrid.DataSource = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Sku,
            Category = _categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? $"#{p.CategoryId}",
            Price = $"{p.Price:N0} ₽",
            p.Stock,
            Image = p.HasImage ? "есть" : "—",
            CrossSell = p.IsCrossSell ? "да" : "—"
        }).ToList();

        // Кеш картинок синхронизируем с текущим каталогом
        ProductImageCache.Instance.Sweep(products);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_loadError != null)
            Toast.Show(this, _loadError, ToastType.Error);
    }

    private void BtnAddUser_Click(object? sender, EventArgs e)
    {
        using var dialog = new UserEditDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var user = new User
        {
            Login = dialog.Login,
            Password = dialog.Password,
            FullName = dialog.FullName,
            Role = dialog.Role
        };

        try
        {
            ApiClient.Instance.AddUser(user);
            LoadData();
            Toast.Show(this, "Пользователь добавлен", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    private void BtnEditUser_Click(object? sender, EventArgs e)
    {
        if (_usersGrid.SelectedRows.Count == 0) return;
        var id = (int)_usersGrid.SelectedRows[0].Cells["Id"].Value;

        User? user;
        try
        {
            user = ApiClient.Instance.GetUsers().FirstOrDefault(u => u.Id == id);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
            return;
        }
        if (user == null) return;

        using var dialog = new UserEditDialog(user.Login, user.Password, user.FullName, user.Role);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        user.Login = dialog.Login;
        user.Password = dialog.Password;
        user.FullName = dialog.FullName;
        user.Role = dialog.Role;

        try
        {
            ApiClient.Instance.UpdateUser(user);
            LoadData();
            Toast.Show(this, "Пользователь обновлен", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    private void BtnDeleteUser_Click(object? sender, EventArgs e)
    {
        if (_usersGrid.SelectedRows.Count == 0) return;
        var id = (int)_usersGrid.SelectedRows[0].Cells["Id"].Value;

        if (id == Session.CurrentUser?.Id)
        {
            Toast.Show(this, "Нельзя удалить самого себя", ToastType.Error);
            return;
        }

        if (!ConfirmDialog.Ask(this, "Подтверждение", "Удалить пользователя?"))
            return;

        try
        {
            ApiClient.Instance.DeleteUser(id);
            LoadData();
            Toast.Show(this, "Пользователь удален", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    private void BtnAddIp_Click(object? sender, EventArgs e)
    {
        using var dialog = new IpEditDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var ip = new IpEntry { IpAddress = dialog.IpAddress, Comment = dialog.Comment };

        try
        {
            ApiClient.Instance.AddIpEntry(ip);
            LoadData();
            Toast.Show(this, "IP добавлен", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    private void BtnRemoveIp_Click(object? sender, EventArgs e)
    {
        if (_ipGrid.SelectedRows.Count == 0) return;
        var id = (int)_ipGrid.SelectedRows[0].Cells["Id"].Value;

        if (!ConfirmDialog.Ask(this, "Подтверждение", "Удалить IP-адрес?"))
            return;

        try
        {
            ApiClient.Instance.DeleteIpEntry(id);
            LoadData();
            Toast.Show(this, "IP удален", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    // ---------- товары ----------

    private void BtnAddProduct_Click(object? sender, EventArgs e)
    {
        using var dialog = new ProductEditDialog(null, _categories);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        LoadData();
        Toast.Show(this, "Товар добавлен", ToastType.Success);
    }

    private void BtnEditProduct_Click(object? sender, EventArgs e)
    {
        var product = GetSelectedProduct();
        if (product is null) return;

        using var dialog = new ProductEditDialog(product, _categories);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        LoadData();
        Toast.Show(this, "Товар обновлён", ToastType.Success);
    }

    private Product? GetSelectedProduct()
    {
        if (_productsGrid.SelectedRows.Count == 0)
        {
            Toast.Show(this, "Выберите товар в списке", ToastType.Warning);
            return null;
        }

        var id = (int)_productsGrid.SelectedRows[0].Cells["Id"].Value;
        try
        {
            return ApiClient.Instance.GetProductById(id);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
            return null;
        }
    }

    private void BtnDeleteProduct_Click(object? sender, EventArgs e)
    {
        if (_productsGrid.SelectedRows.Count == 0)
        {
            Toast.Show(this, "Выберите товар в списке", ToastType.Warning);
            return;
        }

        var id = (int)_productsGrid.SelectedRows[0].Cells["Id"].Value;
        var name = _productsGrid.SelectedRows[0].Cells["Name"].Value?.ToString();

        if (!ConfirmDialog.Ask(this, "Удаление товара",
                $"Удалить товар «{name}»? Изображение тоже будет удалено."))
            return;

        try
        {
            ApiClient.Instance.DeleteProduct(id);
            LoadData();
            Toast.Show(this, "Товар удалён", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }

    private void BtnImportProducts_Click(object? sender, EventArgs e)
    {
        using var dialog = new ProductImportDialog();
        dialog.ShowDialog(this);

        if (dialog.Imported)
        {
            LoadData();
            Toast.Show(this, "Каталог обновлён после импорта", ToastType.Success);
        }
    }

    private void BtnAddCategory_Click(object? sender, EventArgs e)
    {
        using var dialog = new TextInputDialog(
            "Новая категория", "Название категории");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var name = dialog.Value.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            Toast.Show(this, "Укажите название категории", ToastType.Warning);
            return;
        }

        try
        {
            ApiClient.Instance.AddCategory(name);
            LoadData();
            Toast.Show(this, "Категория добавлена", ToastType.Success);
        }
        catch (Exception ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
    }
}

/// <summary>
/// Диалог редактирования пользователя.
/// </summary>
internal class UserEditDialog : ModernForm
{
    private readonly InputField _loginField;
    private readonly InputField _passwordField;
    private readonly InputField _fullNameField;
    private readonly DarkComboBox _roleCombo;

    public string Login => _loginField.Text;
    public string Password => _passwordField.Text;
    public string FullName => _fullNameField.Text;
    public Role Role => (Role)(_roleCombo.SelectedItem ?? Role.Cashier);

    public UserEditDialog(string login = "", string password = "", string fullName = "", Role role = Role.Cashier)
    {
        Text = string.IsNullOrEmpty(login) ? "Новый пользователь" : "Редактировать пользователя";
        Size = new Size(420, 470);
        MinimumSize = new Size(360, 430);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        AllowResize = false;
        Padding = new Padding(0, 40, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            BackColor = Theme.Background,
            Padding = new Padding(24, 8, 24, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 4; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        }

        _loginField = new InputField("Логин", "\uE716") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _passwordField = new InputField("Пароль", "\uE72E", true) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
        _fullNameField = new InputField("ФИО", "\uE716") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };

        _loginField.Text = login;
        _passwordField.Text = password;
        _fullNameField.Text = fullName;

        _roleCombo = new DarkComboBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4)
        };
        _roleCombo.SetItems(Enum.GetValues(typeof(Role)).Cast<object>());
        _roleCombo.SelectedItem = role;

        layout.Controls.Add(CreateCaption("Логин"), 0, 0);
        layout.Controls.Add(_loginField, 0, 1);
        layout.Controls.Add(CreateCaption("Пароль"), 0, 2);
        layout.Controls.Add(_passwordField, 0, 3);
        layout.Controls.Add(CreateCaption("ФИО"), 0, 4);
        layout.Controls.Add(_fullNameField, 0, 5);
        layout.Controls.Add(CreateCaption("Роль"), 0, 6);
        layout.Controls.Add(_roleCombo, 0, 7);

        var btnOk = new FlatButton { Text = "OK", Variant = ButtonVariant.Primary, AutoSize = true, Margin = Padding.Empty };
        btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new FlatButton { Text = "Отмена", Variant = ButtonVariant.Ghost, AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(24, 12, 24, 12)
        };
        btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel });

        Controls.Add(layout);
        Controls.Add(btnPanel);
    }

    private static Label CreateCaption(string text) => new()
    {
        Text = text,
        Font = Theme.SmallFont,
        ForeColor = Theme.TextSecondary,
        AutoSize = true,
        Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
        Margin = Padding.Empty
    };
}

/// <summary>
/// Диалог добавления IP-адреса.
/// </summary>
internal class IpEditDialog : ModernForm
{
    private readonly InputField _ipField;
    private readonly InputField _commentField;

    public string IpAddress => _ipField.Text;
    public string Comment => _commentField.Text;

    public IpEditDialog()
    {
        Text = "Добавить IP-адрес";
        Size = new Size(420, 330);
        MinimumSize = new Size(360, 310);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        AllowResize = false;
        Padding = new Padding(0, 40, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Background,
            Padding = new Padding(24, 8, 24, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

        _ipField = new InputField("IP-адрес (например, 192.168.1.100)", "\uE765")
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4)
        };
        _commentField = new InputField("Комментарий", "\uE70F")
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4)
        };

        layout.Controls.Add(new Label
        {
            Text = "IP-адрес",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Margin = Padding.Empty
        }, 0, 0);
        layout.Controls.Add(_ipField, 0, 1);
        layout.Controls.Add(new Label
        {
            Text = "Комментарий",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Margin = Padding.Empty
        }, 0, 2);
        layout.Controls.Add(_commentField, 0, 3);

        var btnOk = new FlatButton { Text = "OK", Variant = ButtonVariant.Primary, AutoSize = true, Margin = Padding.Empty };
        btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        var btnCancel = new FlatButton { Text = "Отмена", Variant = ButtonVariant.Ghost, AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(24, 12, 24, 12)
        };
        btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel });

        Controls.Add(layout);
        Controls.Add(btnPanel);
    }
}
