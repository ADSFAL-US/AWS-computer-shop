using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.UserControls;

/// <summary>
/// Поле поиска с иконкой и placeholder.
/// </summary>
public class SearchBox : Control
{
    private readonly TextBox _textBox;
    private readonly string _placeholder;
    private bool _focused;

    public SearchBox(string placeholder = "Поиск...")
    {
        _placeholder = placeholder;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | 
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Font = Theme.RegularFont;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            BackColor = Theme.Surface,
            Location = new Point(40, 8),
            Width = 200,
            Height = 24,
            TabStop = true
        };
        _textBox.TextChanged += (s, e) => Invalidate();
        _textBox.Enter += (s, e) => { _focused = true; _textBox.BackColor = Theme.SurfaceElevated; Invalidate(); };
        _textBox.Leave += (s, e) => { _focused = false; _textBox.BackColor = Theme.Surface; Invalidate(); };
        
        Controls.Add(_textBox);
        _textBox.HandleCreated += (s, e) => ApplyCueBanner();
        ApplyCueBanner();

        // Set size AFTER _textBox is created to avoid OnResize accessing null _textBox
        Height = 40;
        MinimumSize = new Size(200, 40);
    }

    private const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    /// <summary>Placeholder рисует сам TextBox — своя отрисовка под ним не видна.</summary>
    private void ApplyCueBanner()
    {
        if (!_textBox.IsHandleCreated) return;
        SendMessage(_textBox.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyCueBanner();
    }

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text 
    { 
        get => _textBox.Text; 
        set => _textBox.Text = value ?? string.Empty; 
    }

    public new event EventHandler? TextChanged
    {
        add => _textBox.TextChanged += value;
        remove => _textBox.TextChanged -= value;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        // Фон
        Color bgColor = _focused ? Theme.SurfaceElevated : Theme.Surface;
        Color borderColor = _focused ? Theme.Accent : Theme.Border;

        // Update TextBox background to match
        _textBox.BackColor = bgColor;

        using (var path = Theme.RoundedRect(rect, 8))
        {
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);
            }
            using (var pen = new Pen(borderColor, _focused ? 2 : 1))
            {
                g.DrawPath(pen, path);
            }
        }

        // Иконка поиска слева
        using (var iconFont = new Font(Theme.IconFontFamily, 16))
        using (var brush = new SolidBrush(_focused ? Theme.Accent : Theme.TextMuted))
        {
            var iconRect = new Rectangle(10, 4, 32, 32);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("\uE721", iconFont, brush, iconRect, sf); // search icon
        }

        // Placeholder теперь рисует сам TextBox (cue banner)
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        _textBox.Focus();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_textBox != null)
        {
            _textBox.Width = Width - 80;
        }
    }

    public void ApplyTheme()
    {
        _textBox.Font = Theme.RegularFont;
        _textBox.ForeColor = Theme.TextPrimary;
        _textBox.BackColor = _focused ? Theme.SurfaceElevated : Theme.Surface;
        Invalidate();
    }
}