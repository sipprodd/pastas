using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Pastas.Domain.Enums;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Diagnostics;
using Pastas.Infrastructure.Files;
using Pastas.Infrastructure.Hotkeys;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Infrastructure.Tray;
using Pastas.Presentation.Design;
using Pastas.Presentation.Services;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private IDiagnosticsLogger _diagnosticsLogger;
    private ClipboardCaptureNotificationHandler? _clipboardCaptureNotificationHandler;
    private IClipboardChangeWatcher? _clipboardChangeWatcher;
    private IGlobalHotkeyService? _hotkeyService;
    private ITrayService? _trayService;

    private bool _isExiting;
    private bool _isCleanedUp;
    private bool _isCompositionInitialized;

    public MainWindow()
    {
        var startupLogger = new FileDiagnosticsLogger();
        _diagnosticsLogger = startupLogger;

        startupLogger.Info("MainWindow ctor: before InitializeComponent().");
        InitializeComponent();
        startupLogger.Info("MainWindow ctor: after InitializeComponent().");

        _diagnosticsLogger.Info("MainWindow ctor: before Loaded event subscription.");
        Loaded += OnLoadedAsync;
        _diagnosticsLogger.Info("MainWindow ctor: after Loaded event subscription.");

        _diagnosticsLogger.Info("MainWindow ctor: before Closing event subscription.");
        Closing += OnClosing;
        _diagnosticsLogger.Info("MainWindow ctor: after Closing event subscription.");

        _diagnosticsLogger.Info("MainWindow ctor: before Closed event subscription.");
        Closed += OnClosedAsync;
        _diagnosticsLogger.Info("MainWindow ctor: after Closed event subscription.");

        Deactivated += OnDeactivated;
        PreviewKeyDown += OnPreviewKeyDown;
    }
    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void SortMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        SortMenuPopup.IsOpen = !SortMenuPopup.IsOpen;
    }

    private void SortRecent_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.Recent, "Recent");
    }

    private void SortOldest_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.Oldest, "Oldest");
    }

    private void SortMostCopied_OnClick(object sender, RoutedEventArgs e)
    {
        SetSortMode(SortMode.MostCopied, "Most copied");
    }

    private void SetSortMode(SortMode sortMode, string label)
    {
        if (_viewModel?.SetSortModeCommand is { } sortCommand && sortCommand.CanExecute(sortMode))
        {
            sortCommand.Execute(sortMode);
        }

        SortMenuButton.Content = label;
        SortMenuPopup.IsOpen = false;
    }


    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        if (e.Key == Key.Space && !_viewModel.IsPreviewOpen)
        {
            _viewModel.OpenPreview();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.IsPreviewOpen)
        {
            _viewModel.ClosePreview();
            e.Handled = true;
        }
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (!_isCompositionInitialized)
        {
            _diagnosticsLogger.Info("MainWindow loaded: before CreateCompositionAsync(this).");
            (_viewModel, _diagnosticsLogger, _clipboardCaptureNotificationHandler, _clipboardChangeWatcher, _hotkeyService, _trayService) = await CreateCompositionAsync(this);
            _diagnosticsLogger.Info("MainWindow loaded: after CreateCompositionAsync(this).");

            _diagnosticsLogger.Info("MainWindow loaded: before DataContext assignment.");
            DataContext = _viewModel;
            _diagnosticsLogger.Info("MainWindow loaded: after DataContext assignment.");

            _diagnosticsLogger.Info("MainWindow loaded: before clipboard watcher event subscription.");
            if (_clipboardCaptureNotificationHandler is not null && _clipboardChangeWatcher is not null)
            {
                _clipboardChangeWatcher.ClipboardChanged += OnClipboardChangedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after clipboard watcher event subscription.");

            _diagnosticsLogger.Info("MainWindow loaded: before hotkey event subscription.");
            if (_hotkeyService is not null)
            {
                _hotkeyService.HotkeyPressed += OnHotkeyPressedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after hotkey event subscription.");

            _diagnosticsLogger.Info("MainWindow loaded: before tray event subscription.");
            if (_trayService is not null)
            {
                _trayService.ShowRequested += OnTrayShowRequestedAsync;
                _trayService.ExitRequested += OnTrayExitRequestedAsync;
            }
            _diagnosticsLogger.Info("MainWindow loaded: after tray event subscription.");

            _isCompositionInitialized = true;
        }

        _clipboardChangeWatcher?.Start();
        _trayService?.Start();

        if (_hotkeyService is not null)
        {
            await _hotkeyService.RegisterAsync("Alt+V");
        }

        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private async void OnClipboardChangedAsync(object? sender, EventArgs e)
    {
        if (_clipboardCaptureNotificationHandler is null)
        {
            return;
        }

        await _clipboardCaptureNotificationHandler.HandleClipboardChangedAsync();
        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }
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

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isExiting || !IsVisible)
        {
            return;
        }

        Hide();
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
        await CleanupAsync();
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

        if (shouldRefresh && _viewModel is not null)
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

        if (_viewModel is not null)
        {
            await _viewModel.RefreshAsync();
        }
    }

    private async Task ExitApplicationAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        await CleanupAsync();
        await Dispatcher.InvokeAsync(() => System.Windows.Application.Current.Shutdown());
    }

    private async Task CleanupAsync()
    {
        if (_isCleanedUp)
        {
            return;
        }

        _isCleanedUp = true;

        if (_clipboardChangeWatcher is not null)
        {
            _clipboardChangeWatcher.ClipboardChanged -= OnClipboardChangedAsync;
            _clipboardChangeWatcher.Stop();
        }

        if (_hotkeyService is not null)
        {
            _hotkeyService.HotkeyPressed -= OnHotkeyPressedAsync;
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

    private static async Task<(MainViewModel ViewModel, IDiagnosticsLogger DiagnosticsLogger, ClipboardCaptureNotificationHandler? NotificationHandler, IClipboardChangeWatcher? Watcher, IGlobalHotkeyService? HotkeyService, ITrayService? TrayService)> CreateCompositionAsync(Window window)
    {
        var diagnosticsLogger = new FileDiagnosticsLogger();

        try
        {
            diagnosticsLogger.Info("CreateComposition: before SqliteDatabasePathProvider.");
            var databasePathProvider = new SqliteDatabasePathProvider();
            diagnosticsLogger.Info("CreateComposition: after SqliteDatabasePathProvider.");

            diagnosticsLogger.Info("CreateComposition: before SqliteConnectionFactory.");
            var connectionFactory = new SqliteConnectionFactory(databasePathProvider);
            diagnosticsLogger.Info("CreateComposition: after SqliteConnectionFactory.");

            diagnosticsLogger.Info("CreateComposition: before SqliteMigrationRunner creation.");
            var migrationRunner = new SqliteMigrationRunner(connectionFactory);
            diagnosticsLogger.Info("CreateComposition: after SqliteMigrationRunner creation.");

            diagnosticsLogger.Info("CreateCompositionAsync: before await migrationRunner.RunAsync().");
            await migrationRunner.RunAsync();
            diagnosticsLogger.Info("CreateCompositionAsync: after await migrationRunner.RunAsync().");

            diagnosticsLogger.Info("CreateComposition: before SqliteClipboardItemRepository.");
            var repository = new SqliteClipboardItemRepository(connectionFactory);
            diagnosticsLogger.Info("CreateComposition: after SqliteClipboardItemRepository.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardRetryPolicy.");
            var retryPolicy = new ClipboardRetryPolicy();
            diagnosticsLogger.Info("CreateComposition: after ClipboardRetryPolicy.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureState.");
            var captureState = new ClipboardCaptureState();
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureState.");

            diagnosticsLogger.Info("CreateComposition: before WindowsClipboardGateway.");
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            diagnosticsLogger.Info("CreateComposition: after WindowsClipboardGateway.");

            diagnosticsLogger.Info("CreateComposition: before LocalFileStorage.");
            var fileStorage = new LocalFileStorage();
            diagnosticsLogger.Info("CreateComposition: after LocalFileStorage.");

            diagnosticsLogger.Info("CreateComposition: before ImageThumbnailBuilder.");
            var thumbnailBuilder = new ImageThumbnailBuilder();
            diagnosticsLogger.Info("CreateComposition: after ImageThumbnailBuilder.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCleanupOptions.");
            var cleanupOptions = new ClipboardCleanupOptions();
            diagnosticsLogger.Info("CreateComposition: after ClipboardCleanupOptions.");

            diagnosticsLogger.Info("CreateComposition: before CaptureClipboardTextUseCase.");
            var captureTextUseCase = new CaptureClipboardTextUseCase(clipboardGateway, repository, captureState, cleanupOptions);
            diagnosticsLogger.Info("CreateComposition: after CaptureClipboardTextUseCase.");

            diagnosticsLogger.Info("CreateComposition: before CaptureClipboardImageUseCase.");
            var captureImageUseCase = new CaptureClipboardImageUseCase(clipboardGateway, repository, fileStorage, thumbnailBuilder, captureState, cleanupOptions);
            diagnosticsLogger.Info("CreateComposition: after CaptureClipboardImageUseCase.");

            diagnosticsLogger.Info("CreateComposition: before CopyTextItemToClipboardUseCase.");
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);
            diagnosticsLogger.Info("CreateComposition: after CopyTextItemToClipboardUseCase.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCleanupService.");
            var cleanupService = new ClipboardCleanupService(repository, fileStorage, cleanupOptions, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCleanupService.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureCoordinator.");
            var coordinator = new ClipboardCaptureCoordinator(captureTextUseCase, captureImageUseCase, cleanupService, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureCoordinator.");

            diagnosticsLogger.Info("CreateComposition: before WindowsClipboardChangeWatcher.");
            var watcher = new WindowsClipboardChangeWatcher(window, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsClipboardChangeWatcher.");

            diagnosticsLogger.Info("CreateComposition: before WindowsHotkeyService.");
            var hotkeyService = new WindowsHotkeyService(window, diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsHotkeyService.");

            diagnosticsLogger.Info("CreateComposition: before WindowsTrayService.");
            var trayService = new WindowsTrayService(diagnosticsLogger);
            diagnosticsLogger.Info("CreateComposition: after WindowsTrayService.");

            diagnosticsLogger.Info("CreateComposition: before MainViewModel.");
            var viewModel = new MainViewModel(repository, copyUseCase);
            diagnosticsLogger.Info("CreateComposition: after MainViewModel.");

            diagnosticsLogger.Info("CreateComposition: before MainViewModelNotificationService.");
            var notificationService = new MainViewModelNotificationService(viewModel, window.Dispatcher);
            diagnosticsLogger.Info("CreateComposition: after MainViewModelNotificationService.");

            diagnosticsLogger.Info("CreateComposition: before ClipboardCaptureNotificationHandler.");
            var notificationHandler = new ClipboardCaptureNotificationHandler(coordinator, notificationService, repository);
            diagnosticsLogger.Info("CreateComposition: after ClipboardCaptureNotificationHandler.");

            diagnosticsLogger.Info("Application composition succeeded.");
            return (viewModel, diagnosticsLogger, notificationHandler, watcher, hotkeyService, trayService);
        }
        catch (Exception ex)
        {
            diagnosticsLogger.Error("Application composition failed.", ex);

            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            var hotkeyService = new WindowsHotkeyService(window, diagnosticsLogger);
            var trayService = new WindowsTrayService(diagnosticsLogger);

            return (new MainViewModel(repository, copyUseCase), diagnosticsLogger, null, null, hotkeyService, trayService);
        }
    }
}


