using System.Windows;
using System.Windows.Threading;

namespace Pastas.Presentation.Notifications;

public sealed class ToastNotificationManager
{
    private const int MaxToasts = 3;
    private const double RightMargin = 16;
    private const double BottomMargin = 16;
    private const double VerticalSpacing = 10;
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
            if (_toasts.Count >= MaxToasts)
            {
                CloseToast(_toasts[0]);
            }

            var toast = new ToastNotificationWindow(title, subtitle);
            toast.Closed += (_, _) => _toasts.Remove(toast);
            _toasts.Add(toast);
            PositionToasts();

            toast.Show();
            toast.PlayShowAnimation(toast.Top);
            _ = AutoDismissAsync(toast);
        });
    }

    private async Task AutoDismissAsync(ToastNotificationWindow toast)
    {
        await Task.Delay(Lifetime);
        await _dispatcher.InvokeAsync(() => CloseToast(toast));
    }

    private void CloseToast(ToastNotificationWindow toast)
    {
        if (!toast.IsVisible)
        {
            return;
        }

        toast.Close();
        PositionToasts();
    }

    private void PositionToasts()
    {
        var workArea = SystemParameters.WorkArea;
        for (var i = 0; i < _toasts.Count; i++)
        {
            var toast = _toasts[i];
            toast.Left = workArea.Right - toast.Width - RightMargin;
            toast.Top = workArea.Bottom - ((i + 1) * toast.Height) - (i * VerticalSpacing) - BottomMargin;
        }
    }
}
