using System.Drawing;
using System.Text;
using ElectronCash.Client.Controls;
using ElectronCash.Client.Models;
using ElectronCash.Client.Services;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Forms;

/// <summary>
/// Импорт товаров вторым способом: на вход base64 от JSON-списка товаров
/// (со ссылками на изображения). Разбор, скачивание картинок и сохранение
/// выполняет бэкенд — здесь только ввод и отчёт.
/// </summary>
internal sealed class ProductImportDialog : ModernForm
{
    private readonly TextBox _payloadBox;
    private readonly Label _hintLabel;
    private readonly DataGridView _resultGrid;

    public bool Imported { get; private set; }

    public ProductImportDialog()
    {
        Text = "Импорт товаров";
        Size = new Size(820, 700);
        MinimumSize = new Size(700, 560);
        StartPosition = FormStartPosition.CenterParent;
        ShowMaximizeButton = false;
        ShowMinimizeButton = false;
        Padding = new Padding(0, 40, 0, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Background,
            Padding = new Padding(20, 12, 20, 0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _hintLabel = new Label
        {
            Text = "Вставьте base64 от JSON-массива товаров. Пример: [{\"name\":\"Товар\",\"price\":100,\"stock\":5,\"category\":\"Наушники\",\"image_url\":\"https://.../photo.jpg\"}]",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _payloadBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9),
            AcceptsReturn = true
        };

        var btnLoadFile = new FlatButton
        {
            Text = "Загрузить из файла",
            Icon = "\uE8E5",
            Variant = ButtonVariant.Secondary,
            AutoSize = true,
            Margin = new Padding(0, 4, 8, 0)
        };
        btnLoadFile.Click += (s, e) => LoadFromFile();

        var btnEncodeJson = new FlatButton
        {
            Text = "Закодировать JSON",
            Icon = "\uE943",
            Variant = ButtonVariant.Secondary,
            AutoSize = true,
            Margin = new Padding(0, 4, 8, 0)
        };
        btnEncodeJson.Click += (s, e) => EncodeJsonInPlace();

        var toolsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        toolsRow.Controls.Add(btnLoadFile);
        toolsRow.Controls.Add(btnEncodeJson);

        _resultGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Border,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            EnableHeadersVisualStyles = false,
            Font = Theme.RegularFont,
            ColumnHeadersHeight = 36,
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
            RowTemplate = { Height = 32 }
        };
        _resultGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Row", HeaderText = "#", FillWeight = 6 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Статус", FillWeight = 14 },
            new DataGridViewTextBoxColumn { Name = "Sku", HeaderText = "Артикул", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Название", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Image", HeaderText = "Фото", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Комментарий", FillWeight = 34 }
        );

        root.Controls.Add(_hintLabel, 0, 0);
        root.Controls.Add(_payloadBox, 0, 1);
        root.Controls.Add(toolsRow, 0, 2);
        root.Controls.Add(_resultGrid, 0, 3);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(20, 14, 20, 14)
        };

        var btnImport = new FlatButton
        {
            Text = "Импортировать",
            Icon = "\uE896",
            Variant = ButtonVariant.Primary,
            AutoSize = true,
            Margin = Padding.Empty
        };
        btnImport.Click += (s, e) => RunImport(btnImport);

        var btnClose = new FlatButton
        {
            Text = "Закрыть",
            Variant = ButtonVariant.Ghost,
            AutoSize = true,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnClose.Click += (s, e) => Close();

        btnPanel.Controls.Add(btnImport);
        btnPanel.Controls.Add(btnClose);

        Controls.Add(root);
        Controls.Add(btnPanel);

        DarkScrollbars.Apply(_payloadBox);
        DarkScrollbars.Apply(_resultGrid);
    }

    private void LoadFromFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Файл со списком товаров (JSON или base64)",
            Filter = "JSON / Base64 (*.json;*.txt;*.b64)|*.json;*.txt;*.b64|Все файлы|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var text = File.ReadAllText(dialog.FileName, Encoding.UTF8).Trim();
            // Если это уже base64 — оставляем как есть, иначе кодируем JSON
            _payloadBox.Text = LooksLikeJson(text)
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes(text))
                : text;
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Не удалось прочитать файл: {ex.Message}", ToastType.Error);
        }
    }

    private static bool LooksLikeJson(string text) =>
        text.StartsWith("[") || text.StartsWith("{");

    private void EncodeJsonInPlace()
    {
        var text = _payloadBox.Text.Trim();
        if (!LooksLikeJson(text))
        {
            Toast.Show(this, "В поле нет JSON для кодирования", ToastType.Warning);
            return;
        }

        _payloadBox.Text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        Toast.Show(this, "JSON закодирован в base64", ToastType.Success);
    }

    private async void RunImport(FlatButton button)
    {
        var payload = _payloadBox.Text.Trim();
        if (string.IsNullOrEmpty(payload))
        {
            Toast.Show(this, "Вставьте base64 со списком товаров", ToastType.Warning);
            return;
        }

        button.Loading = true;
        _resultGrid.Rows.Clear();
        try
        {
            var result = await Task.Run(() => ApiClient.Instance.ImportProducts(payload));
            ShowResult(result);

            Imported |= result.Created > 0 || result.Updated > 0;
            Toast.Show(this,
                $"Импорт: создано {result.Created}, обновлено {result.Updated}, ошибок {result.Failed}",
                result.Failed > 0 ? ToastType.Warning : ToastType.Success);
        }
        catch (ApiException ex)
        {
            Toast.Show(this, ex.Message, ToastType.Error);
        }
        catch (Exception ex)
        {
            Toast.Show(this, $"Ошибка импорта: {ex.Message}", ToastType.Error);
        }
        finally
        {
            button.Loading = false;
        }
    }

    private void ShowResult(ImportResult result)
    {
        _resultGrid.Rows.Clear();
        foreach (var item in result.Items)
        {
            string status = item.Status switch
            {
                "created" => "Создан",
                "updated" => "Обновлён",
                _ => "Ошибка"
            };
            _resultGrid.Rows.Add(
                item.Index + 1,
                status,
                item.Sku,
                item.Name,
                item.ImageSaved ? "есть" : "—",
                item.Message);
        }

        if (result.Items.Count == 0)
            _resultGrid.Rows.Add("—", "Пусто", "", "В списке не было товаров", "—", "");
    }
}
