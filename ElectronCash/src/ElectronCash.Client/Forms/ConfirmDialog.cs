using System.Drawing;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Тёмный аналог MessageBox.YesNo: системный MessageBox всегда светлый
/// и выбивается из оформления приложения.
/// </summary>
internal sealed class ConfirmDialog : ModernForm
{
    private ConfirmDialog(string title, string message, string confirmText, bool danger)
    {
        Text = title;
        Size = new Size(440, 250);
        MinimumSize = new Size(360, 240);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        AllowResize = false;
        Padding = new Padding(0, 40, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Padding = new Padding(24, 8, 24, 16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var text = new Label
        {
            Text = message,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

        var btnOk = new FlatButton
        {
            Text = confirmText,
            Variant = danger ? ButtonVariant.Danger : ButtonVariant.Primary,
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

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        buttons.Controls.Add(btnOk);
        buttons.Controls.Add(btnCancel);

        layout.Controls.Add(text, 0, 0);
        layout.Controls.Add(buttons, 0, 1);
        Controls.Add(layout);

        AcceptButton = null;
        CancelButton = null;
        ActiveControl = btnCancel;
    }

    /// <summary>Спрашивает подтверждение и возвращает true, если пользователь согласился.</summary>
    public static bool Ask(IWin32Window? owner, string title, string message,
        string confirmText = "Удалить", bool danger = true)
    {
        using var dialog = new ConfirmDialog(title, message, confirmText, danger);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }
}
