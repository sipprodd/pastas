namespace Pastas.Application.Services;

public sealed class ClipboardCaptureNotificationHandler
{
    private readonly ClipboardCaptureCoordinator _clipboardCaptureCoordinator;
    private readonly INotificationService _notificationService;

    public ClipboardCaptureNotificationHandler(
        ClipboardCaptureCoordinator clipboardCaptureCoordinator,
        INotificationService notificationService)
    {
        _clipboardCaptureCoordinator = clipboardCaptureCoordinator;
        _notificationService = notificationService;
    }

    public async Task HandleClipboardChangedAsync(CancellationToken cancellationToken = default)
    {
        await _clipboardCaptureCoordinator.CaptureAsync(cancellationToken);

        try
        {
            _notificationService.ShowInfo("Clipboard item saved.");
        }
        catch
        {
            // Notification failure should not crash capture flow.
        }
    }
}
