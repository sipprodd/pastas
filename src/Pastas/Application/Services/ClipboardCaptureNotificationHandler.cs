using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Application.State;

namespace Pastas.Application.Services;

public sealed class ClipboardCaptureNotificationHandler
{
    private readonly ClipboardCaptureCoordinator _clipboardCaptureCoordinator;
    private readonly INotificationService _notificationService;
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly ClipboardCaptureState _captureState;

    public ClipboardCaptureNotificationHandler(
        ClipboardCaptureCoordinator clipboardCaptureCoordinator,
        INotificationService notificationService,
        IClipboardItemRepository clipboardItemRepository,
        ClipboardCaptureState captureState)
    {
        _clipboardCaptureCoordinator = clipboardCaptureCoordinator;
        _notificationService = notificationService;
        _clipboardItemRepository = clipboardItemRepository;
        _captureState = captureState;
    }

    public async Task HandleClipboardChangedAsync(CancellationToken cancellationToken = default)
    {
        if (_captureState.ConsumeInternalClipboardWriteFlag() || _captureState.ConsumeDoNotSaveNextFlag())
        {
            return;
        }

        await _clipboardCaptureCoordinator.CaptureAsync(cancellationToken);

        try
        {
            var totalItems = await _clipboardItemRepository.CountAsync(cancellationToken);
            var items = await _clipboardItemRepository.SearchAsync(new ClipboardSearchQuery
            {
                SortMode = SortMode.Recent,
                Limit = 1,
                Offset = 0
            }, cancellationToken);

            var latest = items.FirstOrDefault();
            var notification = new ClipboardCaptureNotification(
                latest?.Type ?? ClipboardItemType.Text,
                latest?.IsProtected == true,
                totalItems);

            _notificationService.ShowClipboardCaptured(notification);
        }
        catch
        {
            // Notification failure should not crash capture flow.
        }
    }
}
