using ElectronCash.Client.Forms;
using ElectronCash.Client.Services;

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

        AppLog.Info($"Запуск клиента, лог: {AppLog.FilePath}");

        // Global exception handler
        Application.ThreadException += (sender, e) =>
        {
            AppLog.Error("Необработанная ошибка UI-потока", e.Exception);
            var msg = $"Необработанная ошибка:\n{e.Exception.Message}\n\nStackTrace:\n{e.Exception.StackTrace}";
            MessageBox.Show(msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                AppLog.Error("Критическая ошибка", ex);
                var msg = $"Критическая ошибка:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}";
                MessageBox.Show(msg, "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            AppLog.Error("Необработанная ошибка задачи", e.Exception);
            e.SetObserved();
        };

        // Запуск с LoginForm (Этап 3)
        Application.Run(new LoginForm());
    }    
}