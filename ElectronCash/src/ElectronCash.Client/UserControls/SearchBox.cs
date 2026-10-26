using System.Drawing;
using System.Drawing.Drawing2D;
using ElectronCash.Client.Theme;

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
        Height = 40;
        MinimumSize = new Size(200, 40);
        Font = Theme.RegularFont;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.RegularFont,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            Location = new Point(40, 8),
            Width = 200,
            Height = 24,
            TabStop = true
        };
        _textBox.TextChanged += (s, e) => Invalidate();
        _textBox.Enter += (s, e) => { _focused = true; Invalidate(); };
        _textBox.Leave += (s, e) => { _focused = false; Invalidate(); };
        
        Controls.Add(_textBox);
    }

    public override string Text 
    { 
        get => _textBox.Text; 
        set => _textBox.Text = value; 
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
        using (var iconFont = new Font("Segoe MDL2 Assets", 16))
        using (var brush = new SolidBrush(_focused ? Theme.Accent : Theme.TextMuted))
        {
            var iconRect = new Rectangle(10, 4, 32, 32);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("\uE721", iconFont, brush, iconRect, sf); // search icon
        }

        // Placeholder (рисуем если поле пустое и не в фокусе)
        if (string.IsNullOrEmpty(_textBox.Text) && !_focused)
        {
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                var textRect = new Rectangle(40, 0, Width - 80, Height);
                var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                g.DrawString(_placeholder, Theme.RegularFont, brush, textRect, sf);
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        _textBox.Focus();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _textBox.Width = Width - 80;
    }

    public void ApplyTheme()
    {
        _textBox.Font = Theme.RegularFont;
        _textBox.ForeColor = Theme.TextPrimary;
        Invalidate();
    }
}