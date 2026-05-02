using Pastas.Application.UseCases;

namespace Pastas.Application.Services;

public sealed class ClipboardCaptureCoordinator
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(150);

    private readonly ICaptureClipboardTextUseCase _captureClipboardTextUseCase;
    private readonly ICaptureClipboardImageUseCase _captureClipboardImageUseCase;
    private readonly ClipboardCleanupService _clipboardCleanupService;
    private readonly IDiagnosticsLogger? _diagnosticsLogger;
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private readonly TimeSpan _debounceDelay;

    public ClipboardCaptureCoordinator(
        ICaptureClipboardTextUseCase captureClipboardTextUseCase,
        ICaptureClipboardImageUseCase captureClipboardImageUseCase,
        ClipboardCleanupService clipboardCleanupService,
        IDiagnosticsLogger? diagnosticsLogger = null,
        TimeSpan? debounceDelay = null)
    {
        _captureClipboardTextUseCase = captureClipboardTextUseCase;
        _captureClipboardImageUseCase = captureClipboardImageUseCase;
        _clipboardCleanupService = clipboardCleanupService;
        _diagnosticsLogger = diagnosticsLogger;
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
    }

    public async Task CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!await _captureGate.WaitAsync(0, cancellationToken))
        {
            _diagnosticsLogger?.Warning("Clipboard capture skipped: capture already in progress.");
            return;
        }

        _diagnosticsLogger?.Info("Clipboard capture started.");

        try
        {
            await Task.Delay(_debounceDelay, cancellationToken);
            await SafeExecuteAsync("text", () => _captureClipboardTextUseCase.ExecuteAsync(cancellationToken));
            await SafeExecuteAsync("image", () => _captureClipboardImageUseCase.ExecuteAsync(cancellationToken));
            await SafeExecuteAsync("cleanup", () => _clipboardCleanupService.CleanupAsync(cancellationToken));
            _diagnosticsLogger?.Info("Clipboard capture completed.");
        }
        catch (OperationCanceledException)
        {
            _diagnosticsLogger?.Warning("Clipboard capture canceled.");
        }
        catch (Exception ex)
        {
            _diagnosticsLogger?.Error("Clipboard capture failed.", ex);
        }
        finally
        {
            _captureGate.Release();
        }
    }

    private async Task SafeExecuteAsync(string operationName, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _diagnosticsLogger?.Error($"Clipboard capture operation failed: {operationName}.", ex);
        }
    }
}
