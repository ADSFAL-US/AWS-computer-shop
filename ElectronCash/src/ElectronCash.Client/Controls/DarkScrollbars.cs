using System.Runtime.InteropServices;
using ElectronCash.Client.Theming;

namespace ElectronCash.Client.Controls;

/// <summary>
/// Тёмная тема для системных полос прокрутки внутри контролов
/// (DataGridView и прочие рисуют их средствами Windows).
/// </summary>
internal static class DarkScrollbars
{
    private const int WM_NCDESTROY = 0x0082;

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? subAppName, string? subIdList);

    /// <summary>
    /// Пробует включить тёмную полосу прокрутки у контрола и всех его детей.
    /// Не критично: если система не поддерживает — останется системная.
    /// </summary>
    public static void Apply(Control control)
    {
        try
        {
            if (control.IsHandleCreated)
                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);

            control.HandleCreated += (s, e) => Apply(control);

            foreach (Control child in control.Controls)
                Apply(child);
        }
        catch
        {
            // тема не поддерживается — не ломаем интерфейс
        }
    }

    /// <summary>Применяет тёмную тему ко всем сеткам формы.</summary>
    public static void ApplyToGrids(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is DataGridView grid)
            {
                grid.BackgroundColor = Theme.Surface;
                grid.GridColor = Theme.Border;
                Apply(grid);
            }
            ApplyToGrids(child);
        }
    }
}
