using System.Text;

namespace ElectronCash.Client.Services;

/// <summary>
/// Простой файловый лог. Приложение — WinExe, у него нет консоли,
/// поэтому необработанные ошибки иначе видны только в MessageBox.
/// </summary>
public static class AppLog
{
    private const long MaxFileSize = 1024 * 1024;
    private static readonly object _lock = new();

    public static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElectronCash");

    public static string FilePath { get; } = Path.Combine(DirectoryPath, "client.log");

    public static void Info(string message) => Write("INFO", message, null);

    public static void Error(string context, Exception? exception) =>
        Write("ERROR", context, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(DirectoryPath);
                Rotate();

                var builder = new StringBuilder();
                builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append(" [").Append(level).Append("] ")
                    .Append(message)
                    .AppendLine();

                if (exception != null)
                {
                    builder.AppendLine(exception.GetType().FullName + ": " + exception.Message);
                    builder.AppendLine(exception.StackTrace);
                    for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
                        builder.AppendLine($"--- inner: {inner.GetType().FullName}: {inner.Message}");
                }

                File.AppendAllText(FilePath, builder.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Логирование не должно само ронять приложение
        }
    }

    private static void Rotate()
    {
        var info = new FileInfo(FilePath);
        if (!info.Exists || info.Length < MaxFileSize) return;

        var old = FilePath + ".old";
        if (File.Exists(old)) File.Delete(old);
        File.Move(FilePath, old);
    }
}
