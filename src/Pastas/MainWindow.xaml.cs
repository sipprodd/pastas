using System.Windows;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Files;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Infrastructure.Hotkeys;
using Pastas.Presentation.Design;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardCaptureCoordinator? _clipboardCaptureCoordinator;
    private readonly IClipboardChangeWatcher? _clipboardChangeWatcher;
    private readonly IGlobalHotkeyService? _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();
        (_viewModel, _clipboardCaptureCoordinator, _clipboardChangeWatcher, _hotkeyService) = CreateComposition(this);
        DataContext = _viewModel;

        Loaded += OnLoadedAsync;

        if (_clipboardCaptureCoordinator is not null && _clipboardChangeWatcher is not null)
        {
            _clipboardChangeWatcher.ClipboardChanged += OnClipboardChangedAsync;
        }

        _hotkeyService?.HotkeyPressed += OnHotkeyPressedAsync;

        Closed += OnClosedAsync;
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        _clipboardChangeWatcher?.Start();
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

    private async void OnClosedAsync(object? sender, EventArgs e)
    {
        _clipboardChangeWatcher?.Stop();
        if (_hotkeyService is not null)
        {
            await _hotkeyService.UnregisterAsync();
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

    private static (MainViewModel ViewModel, ClipboardCaptureCoordinator? Coordinator, IClipboardChangeWatcher? Watcher, IGlobalHotkeyService? HotkeyService) CreateComposition(Window window)
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

            return (new MainViewModel(repository, copyUseCase), coordinator, watcher, hotkeyService);
        }
        catch
        {
            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            var hotkeyService = new WindowsHotkeyService(window);

            return (new MainViewModel(repository, copyUseCase), null, null, hotkeyService);
        }
    }
}
