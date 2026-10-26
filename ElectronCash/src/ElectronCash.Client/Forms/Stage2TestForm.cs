using ElectronCash.Client.Controls;
using ElectronCash.Client.Theme;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Тестовая форма для демонстрации кастомных контролов Этапа 2.
/// </summary>
public class Stage2TestForm : ModernForm
{
    private readonly CardPanel _card1;
    private readonly CardPanel _card2;
    private readonly FlatButton _btnPrimary;
    private readonly FlatButton _btnSecondary;
    private readonly FlatButton _btnGhost;
    private readonly FlatButton _btnDanger;
    private readonly FlatButton _btnLoading;

    public Stage2TestForm()
    {
        Text = "Этап 2: Кастомные контролы";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;

        // Карточка 1 - Кнопки
        _card1 = new CardPanel
        {
            Location = new Point(24, 60),
            Size = new Size(360, 400),
            ShowShadow = true
        };

        var lblButtons = new Label
        {
            Text = "FlatButton Variants",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        _btnPrimary = new FlatButton
        {
            Text = "Primary",
            Icon = "\uE73E",
            Variant = ButtonVariant.Primary,
            Location = new Point(16, 56),
            Size = new Size(150, 44)
        };

        _btnSecondary = new FlatButton
        {
            Text = "Secondary",
            Icon = "\uE710",
            Variant = ButtonVariant.Secondary,
            Location = new Point(186, 56),
            Size = new Size(150, 44)
        };

        _btnGhost = new FlatButton
        {
            Text = "Ghost",
            Icon = "\uE70F",
            Variant = ButtonVariant.Ghost,
            Location = new Point(16, 116),
            Size = new Size(150, 44)
        };

        _btnDanger = new FlatButton
        {
            Text = "Danger",
            Icon = "\uE711",
            Variant = ButtonVariant.Danger,
            Location = new Point(186, 116),
            Size = new Size(150, 44)
        };

        _btnLoading = new FlatButton
        {
            Text = "Loading...",
            Icon = "\uE72C",
            Variant = ButtonVariant.Primary,
            Location = new Point(16, 176),
            Size = new Size(320, 44)
        };

        _btnLoading.Click += (s, e) =>
        {
            _btnLoading.Loading = true;
            Task.Delay(2000).ContinueWith(_ => 
            {
                if (InvokeRequired)
                {
                    Invoke(() => _btnLoading.Loading = false);
                }
                else
                {
                    _btnLoading.Loading = false;
                }
            });
        };

        var lblToast = new Label
        {
            Text = "Toast Notifications",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 240)
        };

        var btnToastSuccess = new FlatButton
        {
            Text = "Success Toast",
            Variant = ButtonVariant.Primary,
            Location = new Point(16, 280),
            Size = new Size(150, 40)
        };
        btnToastSuccess.Click += (s, e) => Toast.Show(this, "Товар добавлен в корзину!", ToastType.Success);

        var btnToastError = new FlatButton
        {
            Text = "Error Toast",
            Variant = ButtonVariant.Danger,
            Location = new Point(186, 280),
            Size = new Size(150, 40)
        };
        btnToastError.Click += (s, e) => Toast.Show(this, "Ошибка: недостаточно товара", ToastType.Error);

        var btnToastWarning = new FlatButton
        {
            Text = "Warning Toast",
            Variant = ButtonVariant.Secondary,
            Location = new Point(16, 330),
            Size = new Size(150, 40)
        };
        btnToastWarning.Click += (s, e) => Toast.Show(this, "Внимание: мало товара на складе", ToastType.Warning);

        var btnToastInfo = new FlatButton
        {
            Text = "Info Toast",
            Variant = ButtonVariant.Ghost,
            Location = new Point(186, 330),
            Size = new Size(150, 40)
        };
        btnToastInfo.Click += (s, e) => Toast.Show(this, "Информация: смена закрыта", ToastType.Info);

        _card1.Controls.AddRange(new Control[] 
        { 
            lblButtons, _btnPrimary, _btnSecondary, _btnGhost, _btnDanger, _btnLoading,
            lblToast, btnToastSuccess, btnToastError, btnToastWarning, btnToastInfo
        });

        // Карточка 2 - Информация
        _card2 = new CardPanel
        {
            Location = new Point(408, 60),
            Size = new Size(360, 400),
            ShowShadow = true
        };

        var lblInfo = new Label
        {
            Text = "Theme Colors",
            Font = Theme.SectionTitleFont,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var colors = new (string Name, Color Color)[]
        {
            ("Background", Theme.Background),
            ("Surface", Theme.Surface),
            ("SurfaceElevated", Theme.SurfaceElevated),
            ("Accent", Theme.Accent),
            ("AccentHover", Theme.AccentHover),
            ("Danger", Theme.Danger),
            ("Success", Theme.Success),
            ("Warning", Theme.Warning),
            ("Info", Theme.Info),
            ("TextPrimary", Theme.TextPrimary),
            ("TextSecondary", Theme.TextSecondary),
            ("Border", Theme.Border),
        };

        int y = 56;
        foreach (var (name, color) in colors)
        {
            var colorPanel = new Panel
            {
                Location = new Point(16, y),
                Size = new Size(32, 32),
                BackColor = color
            };
            colorPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, 31, 31);
                }
            };

            var colorLabel = new Label
            {
                Text = $"{name}  #{color.R:X2}{color.G:X2}{color.B:X2}",
                Font = Theme.RegularFont,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(56, y + 8)
            };

            _card2.Controls.Add(colorPanel);
            _card2.Controls.Add(colorLabel);
            y += 40;
        }

        Controls.AddRange(new Control[] { _card1, _card2 });
    }
}