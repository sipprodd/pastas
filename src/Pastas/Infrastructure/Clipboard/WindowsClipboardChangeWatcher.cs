using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Pastas.Application.Services;

namespace Pastas.Infrastructure.Clipboard;

public sealed class WindowsClipboardChangeWatcher : IClipboardChangeWatcher
{
    private readonly Window _window;
    private readonly IDiagnosticsLogger? _diagnosticsLogger;
    private HwndSource? _hwndSource;
    private bool _isStarted;

    public WindowsClipboardChangeWatcher(Window window, IDiagnosticsLogger? diagnosticsLogger = null)
    {
        _window = window;
        _diagnosticsLogger = diagnosticsLogger;
    }

    public event EventHandler? ClipboardChanged;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        var windowInteropHelper = new WindowInteropHelper(_window);
        var handle = windowInteropHelper.Handle;
        if (handle == IntPtr.Zero)
        {
            _diagnosticsLogger?.Warning("Watcher start skipped: window handle unavailable.");
            return;
        }

        _hwndSource = HwndSource.FromHwnd(handle);
        if (_hwndSource is null)
        {
            _diagnosticsLogger?.Warning("Watcher start skipped: HwndSource unavailable.");
            return;
        }

        _hwndSource.AddHook(WndProc);
        if (!AddClipboardFormatListener(handle))
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
            _diagnosticsLogger?.Warning("Watcher start failed: AddClipboardFormatListener returned false.");
            return;
        }

        _isStarted = true;
        _diagnosticsLogger?.Info("Watcher started.");
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            return;
        }

        var handle = new WindowInteropHelper(_window).Handle;
        if (handle != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(handle);
        }

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
        _isStarted = false;
        _diagnosticsLogger?.Info("Watcher stopped.");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmClipboardUpdate)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    private const int WmClipboardUpdate = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
}
