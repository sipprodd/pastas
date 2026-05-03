using System.Windows;
using System.Windows.Media.Animation;
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
    private static readonly List<ToastNotificationWindow> ActiveToasts = [];

    private readonly Dispatcher _dispatcher;

    public ToastNotificationManager(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Show(string title, string subtitle)
    {
        _ = _dispatcher.BeginInvoke(() =>
        {
            PruneInactiveToasts();

            while (ActiveToasts.Count >= MaxToasts)
            {
                RemoveToast(ActiveToasts[^1]);
            }

            var toast = new ToastNotificationWindow(title, subtitle);
            toast.Closed += (_, _) => RemoveToast(toast);

            ActiveToasts.Insert(0, toast);
            PositionToasts();

            toast.Show();
            PositionToasts();

            toast.PlayShowAnimation();
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
        for (var i = ActiveToasts.Count - 1; i >= 0; i--)
        {
            var toast = ActiveToasts[i];
            if (toast.IsLoaded && !toast.IsVisible)
            {
                ActiveToasts.RemoveAt(i);
            }
        }

        PositionToasts();
    }

    private void RemoveToast(ToastNotificationWindow toast)
    {
        if (!ActiveToasts.Remove(toast))
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

        for (var i = 0; i < ActiveToasts.Count; i++)
        {
            var toast = ActiveToasts[i];
            toast.BeginAnimation(Window.LeftProperty, null);
            toast.BeginAnimation(Window.TopProperty, null);

            toast.Left = workArea.Right - ToastWidth - RightMargin;
            toast.Top = workArea.Bottom - ((i + 1) * ToastHeight) - (i * VerticalSpacing) - BottomMargin;
        }
    }
}
