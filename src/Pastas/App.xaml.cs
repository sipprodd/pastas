using System.Windows;
using Pastas.Application.Services;
using Pastas.Infrastructure.Diagnostics;

namespace Pastas;

public partial class App : System.Windows.Application
{
    private readonly IDiagnosticsLogger _diagnosticsLogger = new FileDiagnosticsLogger();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _diagnosticsLogger.Info("App startup started.");

        try
        {
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
            _diagnosticsLogger.Info("MainWindow created and shown.");
        }
        catch (Exception ex)
        {
            _diagnosticsLogger.Error("MainWindow creation failed.", ex);
            System.Windows.MessageBox.Show(ex.ToString(), "Pastas startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        _diagnosticsLogger.Error("Unhandled dispatcher exception.", e.Exception);
    }
}
