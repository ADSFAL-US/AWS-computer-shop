using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Административная панель (доступна только для Admin).
/// </summary>
public class AdminForm : ModernForm
{
    private TabControl _tabControl;
        private DataGridView _usersGrid;
        private DataGridView _auditGrid;
        private DataGridView _ipGrid;
        private FlatButton _btnAddUser;
        private FlatButton _btnEditUser;
        private FlatButton _btnDeleteUser;
        private FlatButton _btnAddIp;
        private FlatButton _btnRemoveIp;
        private FlatButton _btnClose;

    public AdminForm()
    {
        Text = "Административная панель";
        Size = new Size(1000, 700);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        Padding = new Padding(0, 40, 0, 0);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Theme.RegularFont,
            ItemSize = new Size(120, 36),
            SizeMode = TabSizeMode.Fixed
        };
        _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabControl.DrawItem += TabControl_DrawItem;

        // Tab: Users
        var tabUsers = new TabPage("Пользователи") { BackColor = Theme.Background };
        SetupUsersTab(tabUsers);
        _tabControl.TabPages.Add(tabUsers);

        // Tab: Audit Log
        var tabAudit = new TabPage("Журнал аудита") { BackColor = Theme.Background };
        SetupAuditTab(tabAudit);
        _tabControl.TabPages.Add(tabAudit);

        // Tab: IP Whitelist
        var tabIp = new TabPage("IP Whitelist") { BackColor = Theme.Background };
        SetupIpTab(tabIp);
        _tabControl.TabPages.Add(tabIp);

        // Close button
        _btnClose = new FlatButton
        {
            Text = "Закрыть",
            Variant = ButtonVariant.Secondary,
            Size = new Size(120, 40),
            Font = Theme.RegularFont,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnClose.Click += (s, e) => Close();

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Theme.Surface,
            Padding = new Padding(16)
        };
        bottomPanel.Controls.Add(_btnClose);
        bottomPanel.Paint += (s, e) =>
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
            }
        };

        Controls.Add(_tabControl);
        Controls.Add(bottomPanel);

        LoadData();
    }

    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        var tabControl = (TabControl)sender!;
        var tabPage = tabControl.TabPages[e.Index];
        var rect = e.Bounds;
        rect.Inflate(-2, -2);

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color bgColor = selected ? Theme.Accent : Theme.Surface;
        Color textColor = selected ? Theme.Background : Theme.TextPrimary;

        using (var path = Theme.RoundedRect(rect, 6))
        using (var brush = new SolidBrush(bgColor))
        {
            e.Graphics.FillPath(brush, path);
        }

        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using (var brush = new SolidBrush(textColor))
        {
            e.Graphics.DrawString(tabPage.Text, Theme.RegularFont, brush, rect, sf);
        }
    }

    private void SetupUsersTab(TabPage tab)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        
        _usersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Border,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            Font = Theme.RegularFont,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.SurfaceElevated,
                ForeColor = Theme.TextPrimary,
                Font = Theme.SectionTitleFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.RegularFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background,
                Padding = new Padding(8)
            },
            RowTemplate = { Height = 40 }
        };
        _usersGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Login", HeaderText = "Логин", DataPropertyName = "Login", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "ФИО", DataPropertyName = "FullName", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Роль", DataPropertyName = "Role", FillWeight = 30 }
        );

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnDeleteUser = new FlatButton
        {
            Text = "Удалить",
            Variant = ButtonVariant.Danger,
            Size = new Size(100, 40),
            Font = Theme.RegularFont
        };
        _btnDeleteUser.Click += BtnDeleteUser_Click;

        _btnEditUser = new FlatButton
        {
            Text = "Изменить",
            Variant = ButtonVariant.Secondary,
            Size = new Size(100, 40),
            Font = Theme.RegularFont
        };
        _btnEditUser.Click += BtnEditUser_Click;

        _btnAddUser = new FlatButton
        {
            Text = "Добавить",
            Variant = ButtonVariant.Primary,
            Size = new Size(100, 40),
            Font = Theme.RegularFont
        };
        _btnAddUser.Click += BtnAddUser_Click;

        btnPanel.Controls.AddRange(new Control[] { _btnAddUser, _btnEditUser, _btnDeleteUser });
        panel.Controls.Add(_usersGrid);
        panel.Controls.Add(btnPanel);
        tab.Controls.Add(panel);
    }

    private void SetupAuditTab(TabPage tab)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        
        _auditGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Border,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            Font = Theme.RegularFont,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.SurfaceElevated,
                ForeColor = Theme.TextPrimary,
                Font = Theme.SectionTitleFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.RegularFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background,
                Padding = new Padding(8)
            },
            RowTemplate = { Height = 40 }
        };
        _auditGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Время", DataPropertyName = "Timestamp", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "UserName", HeaderText = "Пользователь", DataPropertyName = "UserName", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Действие", DataPropertyName = "Action", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "Детали", DataPropertyName = "Details", FillWeight = 30 }
        );

        panel.Controls.Add(_auditGrid);
        tab.Controls.Add(panel);
    }

    private void SetupIpTab(TabPage tab)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        
        _ipGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Border,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            Font = Theme.RegularFont,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.SurfaceElevated,
                ForeColor = Theme.TextPrimary,
                Font = Theme.SectionTitleFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.RegularFont,
                SelectionBackColor = Theme.Accent,
                SelectionForeColor = Theme.Background,
                Padding = new Padding(8)
            },
            RowTemplate = { Height = 40 }
        };
        _ipGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", DataPropertyName = "Id", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "IpAddress", HeaderText = "IP-адрес", DataPropertyName = "IpAddress", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "Comment", HeaderText = "Комментарий", DataPropertyName = "Comment", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "AddedAt", HeaderText = "Добавлен", DataPropertyName = "AddedAt", FillWeight = 20 }
        );

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnRemoveIp = new FlatButton
        {
            Text = "Удалить",
            Variant = ButtonVariant.Danger,
            Size = new Size(100, 40),
            Font = Theme.RegularFont
        };
        _btnRemoveIp.Click += BtnRemoveIp_Click;

        _btnAddIp = new FlatButton
        {
            Text = "Добавить IP",
            Variant = ButtonVariant.Primary,
            Size = new Size(120, 40),
            Font = Theme.RegularFont
        };
        _btnAddIp.Click += BtnAddIp_Click;

        btnPanel.Controls.AddRange(new Control[] { _btnAddIp, _btnRemoveIp });
        panel.Controls.Add(_ipGrid);
        panel.Controls.Add(btnPanel);
        tab.Controls.Add(panel);
    }

    private void LoadData()
    {
        // Users
        _usersGrid.DataSource = MockDataService.Instance.GetUsers().Select(u => new
        {
            u.Id,
            u.Login,
            u.FullName,
            Role = u.Role.ToString()
        }).ToList();

        // Audit
        _auditGrid.DataSource = MockDataService.Instance.GetAuditLog().Select(a => new
        {
            a.Id,
            Timestamp = a.Timestamp.ToString("dd.MM.yyyy HH:mm"),
            a.UserName,
            a.Action,
            a.Details
        }).ToList();

        // IP Whitelist
        _ipGrid.DataSource = MockDataService.Instance.GetIpWhitelist().Select(i => new
        {
            i.Id,
            i.IpAddress,
            i.Comment,
            AddedAt = i.AddedAt.ToString("dd.MM.yyyy HH:mm")
        }).ToList();
    }

    private void BtnAddUser_Click(object? sender, EventArgs e)
    {
        var dialog = new UserEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var user = new User
            {
                Login = dialog.Login,
                Password = dialog.Password,
                FullName = dialog.FullName,
                Role = dialog.Role
            };
            MockDataService.Instance.AddUser(user);
            LoadData();
            Toast.Show(this, "Пользователь добавлен", ToastType.Success);
        }
    }

    private void BtnEditUser_Click(object? sender, EventArgs e)
    {
        if (_usersGrid.SelectedRows.Count == 0) return;
        var id = (int)_usersGrid.SelectedRows[0].Cells["Id"].Value;
        var user = MockDataService.Instance.GetUsers().FirstOrDefault(u => u.Id == id);
        if (user == null) return;

        var dialog = new UserEditDialog(user.Login, user.Password, user.FullName, user.Role);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            user.Login = dialog.Login;
            user.Password = dialog.Password;
            user.FullName = dialog.FullName;
            user.Role = dialog.Role;
            MockDataService.Instance.UpdateUser(user);
            LoadData();
            Toast.Show(this, "Пользователь обновлен", ToastType.Success);
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
        if (MessageBox.Show("Удалить пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            MockDataService.Instance.DeleteUser(id);
            LoadData();
            Toast.Show(this, "Пользователь удален", ToastType.Success);
        }
    }

    private void BtnAddIp_Click(object? sender, EventArgs e)
    {
        var dialog = new IpEditDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var ip = new IpEntry { IpAddress = dialog.IpAddress, Comment = dialog.Comment };
            MockDataService.Instance.AddIpEntry(ip);
            LoadData();
            Toast.Show(this, "IP добавлен", ToastType.Success);
        }
    }

    private void BtnRemoveIp_Click(object? sender, EventArgs e)
    {
        if (_ipGrid.SelectedRows.Count == 0) return;
        var id = (int)_ipGrid.SelectedRows[0].Cells["Id"].Value;
        if (MessageBox.Show("Удалить IP-адрес?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            MockDataService.Instance.DeleteIpEntry(id);
            LoadData();
            Toast.Show(this, "IP удален", ToastType.Success);
        }
    }
}

/// <summary>
/// Диалог редактирования пользователя.
/// </summary>
internal class UserEditDialog : Form
{
    private readonly InputField _loginField;
    private readonly InputField _passwordField;
    private readonly InputField _fullNameField;
    private readonly ComboBox _roleCombo;
    private readonly FlatButton _btnOk;
    private readonly FlatButton _btnCancel;

    public string Login => _loginField.Text;
    public string Password => _passwordField.Text;
    public string FullName => _fullNameField.Text;
    public Role Role => (Role)_roleCombo.SelectedItem!;

    public UserEditDialog(string login = "", string password = "", string fullName = "", Role role = Role.Cashier)
    {
        Text = string.IsNullOrEmpty(login) ? "Новый пользователь" : "Редактировать пользователя";
        Size = new Size(400, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Background;
        Padding = new Padding(24);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 0, 0, 16)
        };

        _loginField = new InputField("Логин", "\uE716") { Width = 340 };
        _passwordField = new InputField("Пароль", "\uE72E", true) { Width = 340 };
        _fullNameField = new InputField("ФИО", "\uE716") { Width = 340 };

        _loginField.Text = login;
        _passwordField.Text = password;
        _fullNameField.Text = fullName;

        _roleCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Theme.RegularFont,
            Width = 340,
            Height = 40,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            DataSource = Enum.GetValues(typeof(Role))
        };
        _roleCombo.SelectedItem = role;

        panel.Controls.Add(new Label { Text = "Логин", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        panel.Controls.Add(_loginField);
        panel.Controls.Add(new Label { Text = "Пароль", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        panel.Controls.Add(_passwordField);
        panel.Controls.Add(new Label { Text = "ФИО", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        panel.Controls.Add(_fullNameField);
        panel.Controls.Add(new Label { Text = "Роль", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        panel.Controls.Add(_roleCombo);

        _btnOk = new FlatButton { Text = "OK", Variant = ButtonVariant.Primary, Size = new Size(100, 40), Font = Theme.RegularFont };
        _btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        _btnCancel = new FlatButton { Text = "Отмена", Variant = ButtonVariant.Ghost, Size = new Size(100, 40), Font = Theme.RegularFont };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        btnPanel.Controls.AddRange(new Control[] { _btnOk, _btnCancel });

        Controls.Add(panel);
        Controls.Add(btnPanel);
    }
}

/// <summary>
/// Диалог редактирования IP.
/// </summary>
internal class IpEditDialog : Form
{
    private readonly InputField _ipField;
    private readonly InputField _commentField;
    private readonly FlatButton _btnOk;
    private readonly FlatButton _btnCancel;

    public string IpAddress => _ipField.Text;
    public string Comment => _commentField.Text;

    public IpEditDialog()
    {
        Text = "Добавить IP-адрес";
        Size = new Size(400, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Background;
        Padding = new Padding(24);

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 0, 0, 16)
        };

        _ipField = new InputField("IP-адрес (например, 192.168.1.100)", "\uE765") { Width = 340 };
        _commentField = new InputField("Комментарий", "\uE70F") { Width = 340 };

        panel.Controls.Add(new Label { Text = "IP-адрес", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        panel.Controls.Add(_ipField);
        panel.Controls.Add(new Label { Text = "Комментарий", Font = Theme.SmallFont, ForeColor = Theme.TextSecondary, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        panel.Controls.Add(_commentField);

        _btnOk = new FlatButton { Text = "OK", Variant = ButtonVariant.Primary, Size = new Size(100, 40), Font = Theme.RegularFont };
        _btnOk.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        _btnCancel = new FlatButton { Text = "Отмена", Variant = ButtonVariant.Ghost, Size = new Size(100, 40), Font = Theme.RegularFont };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        btnPanel.Controls.AddRange(new Control[] { _btnOk, _btnCancel });

        Controls.Add(panel);
        Controls.Add(btnPanel);
    }
}