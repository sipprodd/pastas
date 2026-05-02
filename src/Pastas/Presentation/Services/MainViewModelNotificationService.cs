using System.Windows.Threading;
using Pastas.Application.Services;
using Pastas.Presentation.ViewModels;

namespace Pastas.Presentation.Services;

public sealed class MainViewModelNotificationService : INotificationService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(2.5);

    private readonly MainViewModel _mainViewModel;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;

    public MainViewModelNotificationService(MainViewModel mainViewModel, Dispatcher dispatcher)
    {
        _mainViewModel = mainViewModel;
        _dispatcher = dispatcher;

        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = DefaultDuration
        };
        _timer.Tick += OnTimerTick;
    }

    public void ShowInfo(string message)
    {
        ShowSafe(message);
    }

    public void ShowWarning(string message)
    {
        ShowSafe(message);
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
                // Notification updates should never crash UI flow.
            }
        });
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        _mainViewModel.SetStatusMessage(string.Empty);
    }
}
