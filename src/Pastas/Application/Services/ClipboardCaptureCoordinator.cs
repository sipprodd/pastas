using Pastas.Application.UseCases;

namespace Pastas.Application.Services;

public sealed class ClipboardCaptureCoordinator
{
    private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromMilliseconds(150);

    private readonly ICaptureClipboardTextUseCase _captureClipboardTextUseCase;
    private readonly ICaptureClipboardImageUseCase _captureClipboardImageUseCase;
    private readonly ClipboardCleanupService _clipboardCleanupService;
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private readonly TimeSpan _debounceDelay;

    public ClipboardCaptureCoordinator(
        ICaptureClipboardTextUseCase captureClipboardTextUseCase,
        ICaptureClipboardImageUseCase captureClipboardImageUseCase,
        ClipboardCleanupService clipboardCleanupService,
        TimeSpan? debounceDelay = null)
    {
        _captureClipboardTextUseCase = captureClipboardTextUseCase;
        _captureClipboardImageUseCase = captureClipboardImageUseCase;
        _clipboardCleanupService = clipboardCleanupService;
        _debounceDelay = debounceDelay ?? DefaultDebounceDelay;
    }

    public async Task CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!await _captureGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await Task.Delay(_debounceDelay, cancellationToken);
            await SafeExecuteAsync(() => _captureClipboardTextUseCase.ExecuteAsync(cancellationToken));
            await SafeExecuteAsync(() => _captureClipboardImageUseCase.ExecuteAsync(cancellationToken));
            await SafeExecuteAsync(() => _clipboardCleanupService.CleanupAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _captureGate.Release();
        }
    }

    private static async Task SafeExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch
        {
        }
    }
}
