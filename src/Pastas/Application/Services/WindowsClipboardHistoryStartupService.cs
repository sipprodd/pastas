using Pastas.Domain.Entities;

namespace Pastas.Application.Services;

public sealed class WindowsClipboardHistoryStartupService
{
    private readonly IWindowsClipboardHistoryService _clipboardHistoryService;
    private readonly IDiagnosticsLogger? _diagnosticsLogger;

    public WindowsClipboardHistoryStartupService(
        IWindowsClipboardHistoryService clipboardHistoryService,
        IDiagnosticsLogger? diagnosticsLogger = null)
    {
        _clipboardHistoryService = clipboardHistoryService;
        _diagnosticsLogger = diagnosticsLogger;
    }

    public async Task ApplyAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        if (!settings.ClearWindowsClipboardHistoryOnStartup)
        {
            return;
        }

        try
        {
            await _clipboardHistoryService.ClearHistoryAsync(cancellationToken);
            _diagnosticsLogger?.Info("Windows clipboard history clear requested on startup.");
        }
        catch (Exception ex)
        {
            _diagnosticsLogger?.Warning($"Windows clipboard history clear skipped. ExceptionType={ex.GetType().Name}.");
        }
    }
}
