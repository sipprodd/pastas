using System.Windows;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Files;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Presentation.Design;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ClipboardCaptureCoordinator? _clipboardCaptureCoordinator;
    private readonly IClipboardChangeWatcher? _clipboardChangeWatcher;

    public MainWindow()
    {
        InitializeComponent();
        (_viewModel, _clipboardCaptureCoordinator, _clipboardChangeWatcher) = CreateComposition(this);
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            _clipboardChangeWatcher?.Start();
            await _viewModel.RefreshAsync();
        };

        if (_clipboardCaptureCoordinator is not null && _clipboardChangeWatcher is not null)
        {
            _clipboardChangeWatcher.ClipboardChanged += async (_, _) =>
            {
                await _clipboardCaptureCoordinator.CaptureAsync();
                await _viewModel.RefreshAsync();
            };
        }

        Closed += (_, _) => _clipboardChangeWatcher?.Stop();
    }

    private static (MainViewModel ViewModel, ClipboardCaptureCoordinator? Coordinator, IClipboardChangeWatcher? Watcher) CreateComposition(Window window)
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

            return (new MainViewModel(repository, copyUseCase), coordinator, watcher);
        }
        catch
        {
            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            return (new MainViewModel(repository, copyUseCase), null, null);
        }
    }
}
