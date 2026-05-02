using Pastas.Application.Services;

namespace Pastas.Infrastructure.Diagnostics;

public sealed class FileDiagnosticsLogger : IDiagnosticsLogger
{
    private readonly string _logFilePath;
    private readonly object _syncRoot = new();

    public FileDiagnosticsLogger(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? BuildDefaultLogFilePath();
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var detail = exception is null
            ? message
            : $"{message} | ExceptionType={exception.GetType().Name} Message={exception.Message}";

        Write("ERROR", detail);
    }

    private void Write(string level, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"{DateTime.UtcNow:O} [{level}] {Sanitize(message)}{Environment.NewLine}";

            lock (_syncRoot)
            {
                File.AppendAllText(_logFilePath, line);
            }
        }
        catch
        {
            // Diagnostics should never crash the app.
        }
    }

    private static string BuildDefaultLogFilePath()
    {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(basePath, "Pastas", "logs", "pastas.log");
    }

    private static string Sanitize(string message)
    {
        return message.Replace(Environment.NewLine, " ").Trim();
    }
}
