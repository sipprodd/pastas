namespace Pastas.Application.Services;

public interface IDiagnosticsLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
