namespace Pastas.Application.Services;

public interface IWindowsClipboardHistoryService
{
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
