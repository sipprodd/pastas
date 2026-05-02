namespace Pastas.Domain.Interfaces;

public interface IHotkeyService
{
    Task RegisterAsync(string hotkey, CancellationToken cancellationToken = default);
    Task UnregisterAsync(CancellationToken cancellationToken = default);
}
