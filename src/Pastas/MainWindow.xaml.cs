using System.Windows;
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
            return new MainViewModel(repository);
        }
        catch
        {
            return new MainViewModel(new EmptyClipboardItemRepository());
        }
    }
}
