using ElectronCash.Client.Forms;

namespace ElectronCash.Client;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        // Global exception handler
        Application.ThreadException += (sender, e) =>
        {
            var msg = $"Необработанная ошибка:\n{e.Exception.Message}\n\nStackTrace:\n{e.Exception.StackTrace}";
            MessageBox.Show(msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                var msg = $"Критическая ошибка:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}";
                MessageBox.Show(msg, "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // Запуск с LoginForm (Этап 3)
        Application.Run(new LoginForm());
    }    
}