using System;
using System.Windows;
using System.Windows.Threading;
using Pastas.Application.Services;
using Pastas.Infrastructure.Diagnostics;

namespace Pastas;

public partial class App : System.Windows.Application
{
    private IDiagnosticsLogger? _diagnosticsLogger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _diagnosticsLogger = new FileDiagnosticsLogger();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;

        LogStartup("App OnStartup entered.");

        try
        {
            LogStartup("Creating MainWindow.");
            var window = new MainWindow();
            LogStartup("MainWindow created.");

            MainWindow = window;
            window.Show();
            window.Activate();
            LogStartup("MainWindow shown.");
        }
        catch (Exception ex)
        {
            LogException("MainWindow creation failed.", ex);
            System.Windows.MessageBox.Show("Pastas could not start. Check the diagnostics log for details.", "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("Dispatcher unhandled exception.", e.Exception);
        System.Windows.MessageBox.Show("Pastas encountered an unexpected error and will close. Check the diagnostics log for details.", "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Shutdown();
    }

    private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exceptionType = e.ExceptionObject is Exception ex ? ex.GetType().Name : e.ExceptionObject?.GetType().Name ?? "Unknown";

        _diagnosticsLogger?.Error($"AppDomain unhandled exception. IsTerminating={e.IsTerminating}. ExceptionType={exceptionType}.");

        try
        {
            System.Windows.MessageBox.Show("Pastas encountered a fatal error. Check the diagnostics log for details.", "Fatal exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // No UI dispatcher available; exception is already logged.
        }
    }

    private void LogStartup(string message)
    {
        _diagnosticsLogger?.Info(message);
    }

    private void LogException(string message, Exception ex)
    {
        _diagnosticsLogger?.Error(message, ex);
    }
}
