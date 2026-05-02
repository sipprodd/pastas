using System.ComponentModel;
using System.Windows;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Files;
using Pastas.Infrastructure.Hotkeys;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Infrastructure.Tray;
using Pastas.Presentation.Design;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardCaptureCoordinator? _clipboardCaptureCoordinator;
    private readonly IClipboardChangeWatcher? _clipboardChangeWatcher;
    private readonly IGlobalHotkeyService? _hotkeyService;
    private readonly ITrayService? _trayService;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();
        (_viewModel, _clipboardCaptureCoordinator, _clipboardChangeWatcher, _hotkeyService, _trayService) = CreateComposition(this);
        DataContext = _viewModel;

        Loaded += OnLoadedAsync;

        if (_clipboardCaptureCoordinator is not null && _clipboardChangeWatcher is not null)
        {
            _clipboardChangeWatcher.ClipboardChanged += OnClipboardChangedAsync;
        }

        _hotkeyService?.HotkeyPressed += OnHotkeyPressedAsync;

        if (_trayService is not null)
        {
            _trayService.ShowRequested += OnTrayShowRequestedAsync;
            _trayService.ExitRequested += OnTrayExitRequestedAsync;
        }

        Closing += OnClosing;
        Closed += OnClosedAsync;
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        _clipboardChangeWatcher?.Start();
        _trayService?.Start();

        if (_hotkeyService is not null)
        {
            await _hotkeyService.RegisterAsync("Alt+V");
        }

        await _viewModel.RefreshAsync();
    }

    private async void OnClipboardChangedAsync(object? sender, EventArgs e)
    {
        if (_clipboardCaptureCoordinator is null)
        {
            return;
        }

        await _clipboardCaptureCoordinator.CaptureAsync();
        await _viewModel.RefreshAsync();
    }

    private async void OnHotkeyPressedAsync(object? sender, EventArgs e)
    {
        await ToggleWindowVisibilityAsync();
    }

    private async void OnTrayShowRequestedAsync(object? sender, EventArgs e)
    {
        await ShowWindowAsync();
    }

    private async void OnTrayExitRequestedAsync(object? sender, EventArgs e)
    {
        await ExitApplicationAsync();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        _clipboardChangeWatcher?.Stop();
        if (_hotkeyService is not null)
        {
            await _hotkeyService.UnregisterAsync();
        }

        if (_trayService is not null)
        {
            _trayService.ShowRequested -= OnTrayShowRequestedAsync;
            _trayService.ExitRequested -= OnTrayExitRequestedAsync;
            _trayService.Stop();
            _trayService.Dispose();
        }
    }

    private async Task ToggleWindowVisibilityAsync()
    {
        var shouldRefresh = false;

        await Dispatcher.InvokeAsync(() =>
        {
            if (IsVisible && IsActive)
            {
                Hide();
                return;
            }

            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            shouldRefresh = true;
        });

        if (shouldRefresh)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private async Task ShowWindowAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
        });

        await _viewModel.RefreshAsync();
    }

    private async Task ExitApplicationAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        _clipboardChangeWatcher?.Stop();
        if (_hotkeyService is not null)
        {
            await _hotkeyService.UnregisterAsync();
        }

        if (_trayService is not null)
        {
            _trayService.ShowRequested -= OnTrayShowRequestedAsync;
            _trayService.ExitRequested -= OnTrayExitRequestedAsync;
            _trayService.Stop();
            _trayService.Dispose();
        }

        await Dispatcher.InvokeAsync(() => Application.Current.Shutdown());
    }

    private static (MainViewModel ViewModel, ClipboardCaptureCoordinator? Coordinator, IClipboardChangeWatcher? Watcher, IGlobalHotkeyService? HotkeyService, ITrayService? TrayService) CreateComposition(Window window)
    {
        try
        {
            var databasePathProvider = new SqliteDatabasePathProvider();
            var connectionFactory = new SqliteConnectionFactory(databasePathProvider);
            var migrationRunner = new SqliteMigrationRunner(connectionFactory);
            migrationRunner.RunAsync().GetAwaiter().GetResult();

            var repository = new SqliteClipboardItemRepository(connectionFactory);
            var retryPolicy = new ClipboardRetryPolicy();
            var captureState = new ClipboardCaptureState();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var fileStorage = new LocalFileStorage();
            var thumbnailBuilder = new ImageThumbnailBuilder();

            var captureTextUseCase = new CaptureClipboardTextUseCase(clipboardGateway, repository, captureState);
            var captureImageUseCase = new CaptureClipboardImageUseCase(clipboardGateway, repository, fileStorage, thumbnailBuilder, captureState);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);
            var coordinator = new ClipboardCaptureCoordinator(captureTextUseCase, captureImageUseCase);
            var watcher = new WindowsClipboardChangeWatcher(window);

            var hotkeyService = new WindowsHotkeyService(window);
            var trayService = new WindowsTrayService();

            return (new MainViewModel(repository, copyUseCase), coordinator, watcher, hotkeyService, trayService);
        }
        catch
        {
            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            var hotkeyService = new WindowsHotkeyService(window);
            var trayService = new WindowsTrayService();

            return (new MainViewModel(repository, copyUseCase), null, null, hotkeyService, trayService);
        }
    }
}
