using System.Windows;
using System.Windows.Threading;

namespace Pastas.Presentation.Notifications;

public sealed class ToastNotificationManager
{
    private const int MaxToasts = 3;
    private const double RightMargin = 16;
    private const double BottomMargin = 16;
    private const double VerticalSpacing = 10;
    private const double FallbackWidth = 300;
    private const double FallbackHeight = 82;
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
            RemoveInvisibleToasts();

            while (_toasts.Count >= MaxToasts)
            {
                RemoveToast(_toasts[^1]);
            }

            var toast = new ToastNotificationWindow(title, subtitle);
            toast.Closed += (_, _) =>
            {
                _toasts.Remove(toast);
                PositionToasts();
            };

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

    private void RemoveInvisibleToasts()
    {
        for (var i = _toasts.Count - 1; i >= 0; i--)
        {
            if (!_toasts[i].IsVisible)
            {
                _toasts.RemoveAt(i);
            }
        }
    }

    private void RemoveToast(ToastNotificationWindow toast)
    {
        _toasts.Remove(toast);

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
            var width = toast.Width > 0 ? toast.Width : FallbackWidth;
            var height = toast.Height > 0 ? toast.Height : FallbackHeight;

            toast.Left = workArea.Right - width - RightMargin;
            toast.Top = workArea.Bottom - ((i + 1) * height) - (i * VerticalSpacing) - BottomMargin;
        }
    }
}
