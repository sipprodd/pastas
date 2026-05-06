namespace Pastas.Application.Services;

public interface ITrayService : IDisposable
{
    event EventHandler? ShowRequested;
    event EventHandler? SettingsRequested;
    event EventHandler? CapturePauseToggleRequested;
    event EventHandler? ExitRequested;

    void Start();
    void Stop();
    void SetCapturePaused(bool isPaused);
}
