namespace Pastas.Application.Services;

public interface INotificationService
{
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowClipboardCaptured(ClipboardCaptureNotification notification);
}
