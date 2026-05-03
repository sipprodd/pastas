using System.Windows.Threading;
using Pastas.Application.Services;
using Pastas.Presentation.Notifications;
using Pastas.Presentation.ViewModels;

namespace Pastas.Presentation.Services;

public sealed class MainViewModelNotificationService : INotificationService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2.5);

    private readonly MainViewModel _mainViewModel;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private readonly ToastNotificationManager _toastManager;

    public MainViewModelNotificationService(MainViewModel mainViewModel, Dispatcher dispatcher)
    {
        _mainViewModel = mainViewModel;
        _dispatcher = dispatcher;
        _toastManager = new ToastNotificationManager(_dispatcher);

        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = DefaultDuration
        };
        _timer.Tick += OnTimerTick;
    }

    public void ShowInfo(string message) => ShowSafe(message);

    public void ShowWarning(string message) => ShowSafe(message);

    public void ShowClipboardCaptured(ClipboardCaptureNotification notification)
    {
        ShowSafe(notification.Title);
        _toastManager.Show(notification.Title, notification.Subtitle);
    }

    private void ShowSafe(string message)
    {
        _ = _dispatcher.BeginInvoke(() =>
        {
            try
            {
                _mainViewModel.SetStatusMessage(message);
                _timer.Stop();
                _timer.Start();
            }
            catch
            {
            }
        });
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        _mainViewModel.SetStatusMessage(string.Empty);
    }
}
