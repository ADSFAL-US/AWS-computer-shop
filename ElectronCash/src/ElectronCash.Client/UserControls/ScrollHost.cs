using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Контейнер с прокруткой и собственным тонким скроллбаром
/// (системный скроллбар WinForms нельзя перекрасить под тёмную тему).
/// </summary>
public class ScrollHost : Control
{
    private const int BarWidth = 8;
    private const int BarMargin = 4;
    private const int MinThumbHeight = 32;

    private readonly Panel _content;
    private int _offset;
    private bool _dragging;
    private int _dragStartY;
    private int _dragStartOffset;
    private bool _barHover;
    private WheelFilter? _wheelFilter;

    public ScrollHost()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = Theme.Background;

        _content = new Panel
        {
            Location = new Point(0, 0),
            BackColor = Theme.Background,
            AutoSize = false
        };
        _content.ControlAdded += (s, e) => UpdateScrollRange();
        _content.ControlRemoved += (s, e) => UpdateScrollRange();
        _content.SizeChanged += (s, e) => UpdateScrollRange();

        _content.BackColor = BackColor;
        Controls.Add(_content);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _wheelFilter = new WheelFilter(this);
        Application.AddMessageFilter(_wheelFilter);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_wheelFilter != null)
        {
            Application.RemoveMessageFilter(_wheelFilter);
            _wheelFilter = null;
        }
        base.OnHandleDestroyed(e);
    }

    /// <summary>Прокрутка колесом мыши над любым дочерним элементом.</summary>
    private sealed class WheelFilter : IMessageFilter
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        private const uint GA_ROOT = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);

        private readonly ScrollHost _host;

        public WheelFilter(ScrollHost host) => _host = host;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL) return false;
            if (!_host.IsHandleCreated || !_host.Visible) return false;

            var cursor = Cursor.Position;
            if (!_host.RectangleToScreen(_host.ClientRectangle).Contains(cursor)) return false;

            // Приложение рисует диалоги как обычные окна поверх главной формы,
            // поэтому одной геометрии мало: иначе прокрутка «протекает» сквозь
            // открытую админку/диалог в каталог под ним. Сверяем корневое окно
            // под курсором с корневым окном нашей формы.
            var topWindow = GetAncestor(WindowFromPoint(new NativePoint { X = cursor.X, Y = cursor.Y }), GA_ROOT);
            var form = _host.FindForm();
            if (form is null || !form.IsHandleCreated) return false;
            if (topWindow != GetAncestor(form.Handle, GA_ROOT)) return false;

            int delta = (short)((long)m.WParam >> 16);
            _host.ScrollBy(-delta);
            return true;
        }
    }

    /// <summary>Панель, в которую складывается прокручиваемое содержимое.</summary>
    public Panel ContentPanel => _content;

    /// <summary>Отступ содержимого от правого края (место под скроллбар).</summary>
    public int ContentRightPadding { get; set; } = BarWidth + BarMargin * 2;

    /// <summary>
    /// Ширина, которую должно занимать содержимое. Полосу прокрутки
    /// резервируем только когда она реально нужна, иначе справа
    /// оставалась бы полоса другого цвета.
    /// </summary>
    public int ContentWidth => Math.Max(
        0, ClientSize.Width - (NeedsBar ? ContentRightPadding : 0));

    /// <summary>Фон содержимого повторяет фон контейнера.</summary>
    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_content != null)
            _content.BackColor = BackColor;
    }

    private int MaxOffset => Math.Max(0, _content.Height - ClientSize.Height);

    private bool NeedsBar => MaxOffset > 0;

    private Rectangle TrackRect => new(Width - BarWidth - BarMargin, BarMargin, BarWidth, Height - BarMargin * 2);
    private Rectangle ThumbRect
    {
        get
        {
            var track = TrackRect;
            if (!NeedsBar) return Rectangle.Empty;

            int contentHeight = Math.Max(_content.Height, 1);
            int thumbHeight = Math.Max(MinThumbHeight, track.Height * ClientSize.Height / contentHeight);
            thumbHeight = Math.Min(thumbHeight, track.Height);

            int travel = track.Height - thumbHeight;
            int top = track.Top + (MaxOffset == 0 ? 0 : travel * _offset / MaxOffset);
            return new Rectangle(track.X, top, track.Width, thumbHeight);
        }
    }

    /// <summary>Пересчитывает размеры содержимого и позицию прокрутки.</summary>
    public void UpdateScrollRange()
    {
        int contentWidth = ContentWidth;
        if (_content.Width != contentWidth)
            _content.Width = contentWidth;

        _content.PerformLayout();

        int contentHeight = 0;
        foreach (Control child in _content.Controls)
            contentHeight = Math.Max(contentHeight, child.Bottom + child.Margin.Bottom);

        if (_content.Height != contentHeight)
            _content.Height = contentHeight;

        SetOffset(_offset);
        Invalidate();
    }

    /// <summary>Прокручивает в начало.</summary>
    public void ScrollToTop() => SetOffset(0);

    /// <summary>Прокручивает на указанное число пикселей.</summary>
    public void ScrollBy(int delta) => SetOffset(_offset + delta);

    private void SetOffset(int value)
    {
        _offset = Math.Clamp(value, 0, MaxOffset);
        _content.Location = new Point(0, -_offset);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateScrollRange();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!NeedsBar) return;

        SetOffset(_offset - e.Delta);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !NeedsBar) return;

        if (ThumbRect.Contains(e.Location))
        {
            _dragging = true;
            _dragStartY = e.Y;
            _dragStartOffset = _offset;
        }
        else if (TrackRect.Contains(e.Location))
        {
            SetOffset(_offset + (e.Y < ThumbRect.Top ? -ClientSize.Height : ClientSize.Height));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        bool hover = TrackRect.Contains(e.Location);
        if (hover != _barHover)
        {
            _barHover = hover;
            Invalidate();
        }

        if (!_dragging) return;

        int travel = TrackRect.Height - ThumbRect.Height;
        if (travel <= 0) return;

        int delta = e.Y - _dragStartY;
        SetOffset(_dragStartOffset + delta * MaxOffset / travel);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _barHover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var brush = new SolidBrush(BackColor))
            g.FillRectangle(brush, ClientRectangle);

        if (!NeedsBar) return;

        var track = TrackRect;
        using (var path = Theme.RoundedRect(track, BarWidth / 2))
        using (var brush = new SolidBrush(Theme.Surface))
        {
            g.FillPath(brush, path);
        }

        var thumb = ThumbRect;
        using (var path = Theme.RoundedRect(thumb, BarWidth / 2))
        using (var brush = new SolidBrush(_barHover || _dragging ? Theme.TextSecondary : Theme.Border))
        {
            g.FillPath(brush, path);
        }
    }
}
