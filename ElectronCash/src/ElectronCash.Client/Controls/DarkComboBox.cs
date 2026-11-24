using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Тёмный выпадающий список: системный ComboBox нельзя перекрасить
/// (его поле и список рисуются Windows), поэтому рисуем сами.
/// </summary>
public class DarkComboBox : Control
{
    private readonly List<object> _items = new();
    private int _selectedIndex = -1;
    private bool _hover;
    private bool _open;
    private ToolStripDropDown? _dropDown;

    public DarkComboBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Height = 40;
        MinimumSize = new Size(120, 36);
        MaximumSize = new Size(int.MaxValue, 44);
        Font = Theme.RegularFont;
        Cursor = Cursors.Hand;
    }

    /// <summary>Элементы списка.</summary>
    public IReadOnlyList<object> Items => _items;

    /// <summary>Выбранный элемент (null, если ничего не выбрано).</summary>
    public object? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
        set
        {
            int index = value == null ? -1 : _items.IndexOf(value);
            if (index == _selectedIndex) return;
            _selectedIndex = index;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Индекс выбранного элемента.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SelectedItem = value >= 0 && value < _items.Count ? _items[value] : null;
    }

    /// <summary>Событие смены выбора.</summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>Заполняет список элементами.</summary>
    public void SetItems(IEnumerable<object> items)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = _items.Count > 0 ? 0 : -1;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        Color bg = _open || _hover ? Theme.SurfaceElevated : Theme.Surface;
        Color border = _open ? Theme.Accent : Theme.Border;

        using (var path = Theme.RoundedRect(rect, 8))
        {
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);
            using (var pen = new Pen(border, _open ? 2 : 1))
                g.DrawPath(pen, path);
        }

        var textRect = new Rectangle(12, 0, rect.Width - 48, rect.Height);
        using (var brush = new SolidBrush(Theme.TextPrimary))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            g.DrawString(SelectedItem?.ToString() ?? string.Empty, Font, brush, textRect, sf);
        }

        using (var iconFont = new Font(Theme.IconFontFamily, 10))
        using (var brush = new SolidBrush(_open ? Theme.Accent : Theme.TextSecondary))
        {
            var iconRect = new Rectangle(rect.Width - 32, 0, 24, rect.Height);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("\uE70D", iconFont, brush, iconRect, sf);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
            ToggleDropDown();
    }

    private void ToggleDropDown()
    {
        if (_open)
        {
            CloseDropDown();
            return;
        }

        if (_items.Count == 0) return;

        DisposeDropDown();

        var list = new ListBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.SurfaceElevated,
            ForeColor = Theme.TextPrimary,
            Font = Theme.RegularFont,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 32,
            IntegralHeight = false,
            Width = Width,
            Height = Math.Min(_items.Count * 32 + 4, 240)
        };
        list.Items.AddRange(_items.ToArray());
        list.SelectedIndex = _selectedIndex;

        list.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using var bg = new SolidBrush(selected ? Theme.Accent : Theme.SurfaceElevated);
            e.Graphics.FillRectangle(bg, e.Bounds);
            using var fg = new SolidBrush(selected ? Theme.Background : Theme.TextPrimary);
            var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(list.Items[e.Index]?.ToString(), Theme.RegularFont, fg, textRect, sf);
        };

        list.MouseClick += (s, e) =>
        {
            int index = list.IndexFromPoint(e.Location);
            if (index >= 0)
            {
                SelectedIndex = index;
                // Закрываем не в обработчике клика ListBox: уничтожение родительского
                // ToolStripDropDown прямо в его же стеке вызовов даёт ObjectDisposedException
                if (!IsDisposed)
                    BeginInvoke(CloseDropDown);
            }
        };

        var host = new ToolStripControlHost(list)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = list.Size
        };

        _dropDown = new ToolStripDropDown
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            AutoSize = false,
            BackColor = Theme.SurfaceElevated,
            DropShadowEnabled = true
        };
        _dropDown.Items.Add(host);
        _dropDown.Size = list.Size;
        _dropDown.Closed += (s, e) =>
        {
            _open = false;
            Invalidate();
            // Уничтожаем дропдаун отложенно: Closed может прийти из середины
            // обработки клика по ListBox, и Dispose здесь ломает стек вызовов
            if (!IsDisposed)
                BeginInvoke(DisposeDropDown);
        };

        _open = true;
        Invalidate();
        _dropDown.Show(this, new Point(0, Height + 4));
    }

    private void DisposeDropDown()
    {
        var dropDown = _dropDown;
        _dropDown = null;
        if (dropDown == null) return;

        _open = false;
        dropDown.Dispose();
        Invalidate();
    }

    private void CloseDropDown()
    {
        var dropDown = _dropDown;
        if (dropDown == null)
        {
            _open = false;
            Invalidate();
            return;
        }

        _open = false;
        dropDown.Close();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var dropDown = _dropDown;
            _dropDown = null;
            dropDown?.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ApplyTheme()
    {
        Font = Theme.RegularFont;
        Invalidate();
    }
}
