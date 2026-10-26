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
            MessageBox.Show($"Необработанная ошибка: {e.Exception.Message}", "Ошибка", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Критическая ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // Запуск с LoginForm (Этап 3)
        Application.Run(new LoginForm());
    }    
}