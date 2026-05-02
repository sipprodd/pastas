namespace Pastas.Application.Services;

public interface ITrayService : IDisposable
{
    event EventHandler? ShowRequested;
    event EventHandler? ExitRequested;

    void Start();
    void Stop();
}
