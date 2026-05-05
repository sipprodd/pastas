using System.Drawing;
using Pastas.Application.Services;

namespace Pastas.Infrastructure.Tray;

public sealed class WindowsTrayService : ITrayService
{
    private readonly IDiagnosticsLogger? _diagnosticsLogger;
    private NotifyIcon? _notifyIcon;
    private bool _isStarted;

    public event EventHandler? ShowRequested;
    public event EventHandler? SettingsRequested;
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
            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Settings", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

            var icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty) ?? SystemIcons.Application;

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "Pastas",
                Icon = icon,
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.MouseClick += OnNotifyIconMouseClick;
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

        _notifyIcon.MouseClick -= OnNotifyIconMouseClick;
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

    private void OnNotifyIconMouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Left)
        {
            ShowRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
