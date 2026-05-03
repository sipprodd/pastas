using System.Windows;
using System.Windows.Threading;

namespace Pastas.Presentation.Notifications;

public sealed class ToastNotificationManager
{
    private const int MaxToasts = 3;
    private const double RightMargin = 16;
    private const double BottomMargin = 16;
    private const double VerticalSpacing = 10;
    private const double ToastWidth = 300;
    private const double ToastHeight = 82;

    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(2.5);

    private readonly Dispatcher _dispatcher;
    private readonly List<ToastNotificationWindow> _toasts = [];

    public ToastNotificationManager(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Show(string title, string subtitle)
    {
        _ = _dispatcher.BeginInvoke(() =>
        {
            PruneInactiveToasts();

            while (_toasts.Count >= MaxToasts)
            {
                RemoveToast(_toasts[^1]);
            }

            var toast = new ToastNotificationWindow(title, subtitle);
            toast.Closed += (_, _) => RemoveToast(toast);

            _toasts.Insert(0, toast);
            PositionToasts();

            toast.Show();
            PositionToasts();

            toast.PlayShowAnimation(toast.Top);
            _ = AutoDismissAsync(toast);
        });
    }

    private async Task AutoDismissAsync(ToastNotificationWindow toast)
    {
        await Task.Delay(Lifetime);
        await _dispatcher.InvokeAsync(() => RemoveToast(toast));
    }

    private void PruneInactiveToasts()
    {
        for (var i = _toasts.Count - 1; i >= 0; i--)
        {
            var toast = _toasts[i];
            if (toast.IsLoaded && !toast.IsVisible)
            {
                _toasts.RemoveAt(i);
            }
        }

        PositionToasts();
    }

    private void RemoveToast(ToastNotificationWindow toast)
    {
        if (!_toasts.Remove(toast))
        {
            return;
        }

        if (toast.IsVisible)
        {
            toast.Close();
        }

        PositionToasts();
    }

    private void PositionToasts()
    {
        var workArea = SystemParameters.WorkArea;

        for (var i = 0; i < _toasts.Count; i++)
        {
            var toast = _toasts[i];
            toast.Left = workArea.Right - ToastWidth - RightMargin;
            toast.Top = workArea.Bottom - ((i + 1) * ToastHeight) - (i * VerticalSpacing) - BottomMargin;
        }
    }
}
