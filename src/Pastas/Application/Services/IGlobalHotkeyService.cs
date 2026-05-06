namespace Pastas.Application.Services;

public interface IGlobalHotkeyService
{
    event EventHandler? HotkeyPressed;

    Task RegisterAsync(string hotkey, CancellationToken cancellationToken = default);

    Task UnregisterAsync(CancellationToken cancellationToken = default);
}
