using System.Drawing;
using System.Windows.Forms;
using Pastas.Application.Services;

namespace Pastas.Infrastructure.Tray;

public sealed class WindowsTrayService : ITrayService
{
    private readonly IDiagnosticsLogger? _diagnosticsLogger;
    private NotifyIcon? _notifyIcon;
    private bool _isStarted;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public WindowsTrayService(IDiagnosticsLogger? diagnosticsLogger = null)
    {
        _diagnosticsLogger = diagnosticsLogger;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        try
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Pastas", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _notifyIcon = new NotifyIcon
            {
                Text = "Pastas",
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;
            _isStarted = true;
            _diagnosticsLogger?.Info("Tray started.");
        }
        catch (Exception ex)
        {
            _diagnosticsLogger?.Error("Tray start failed.", ex);
            Stop();
        }
    }

    public void Stop()
    {
        if (_notifyIcon is null)
        {
            _isStarted = false;
            return;
        }

        _notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
        _notifyIcon.Visible = false;

        if (_notifyIcon.ContextMenuStrip is not null)
        {
            _notifyIcon.ContextMenuStrip.Dispose();
        }

        _notifyIcon.Dispose();
        _notifyIcon = null;
        _isStarted = false;
        _diagnosticsLogger?.Info("Tray stopped.");
    }

    public void Dispose()
    {
        if (!_isStarted && _notifyIcon is null)
        {
            GC.SuppressFinalize(this);
            return;
        }

        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
    {
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }
}
