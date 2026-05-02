using System.Windows;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Infrastructure.Clipboard;
using Pastas.Infrastructure.Storage.SQLite;
using Pastas.Presentation.Design;
using Pastas.Presentation.ViewModels;

namespace Pastas;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = CreateViewModel();

        Loaded += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.RefreshAsync();
            }
        };
    }

    private static MainViewModel CreateViewModel()
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
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            return new MainViewModel(repository, copyUseCase);
        }
        catch
        {
            var repository = new EmptyClipboardItemRepository();
            var captureState = new ClipboardCaptureState();
            var retryPolicy = new ClipboardRetryPolicy();
            var clipboardGateway = new WindowsClipboardGateway(retryPolicy);
            var copyUseCase = new CopyTextItemToClipboardUseCase(repository, clipboardGateway, captureState);

            return new MainViewModel(repository, copyUseCase);
        }
    }
}
