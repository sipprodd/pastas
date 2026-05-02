using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Pastas.Application.Services;
using Pastas.Domain.Interfaces;

namespace Pastas.Infrastructure.Hotkeys;

public sealed class WindowsHotkeyService : IHotkeyService, IGlobalHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;

    private readonly Window _window;
    private readonly IDiagnosticsLogger? _diagnosticsLogger;
    private HwndSource? _source;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    public WindowsHotkeyService(Window window, IDiagnosticsLogger? diagnosticsLogger = null)
    {
        _window = window;
        _diagnosticsLogger = diagnosticsLogger;
    }

    public Task RegisterAsync(string hotkey, CancellationToken cancellationToken = default)
    {
        if (_isRegistered)
        {
            return Task.CompletedTask;
        }

        if (!HotkeyGestureParser.TryParse(hotkey, out var modifiers, out var virtualKey))
        {
            _diagnosticsLogger?.Warning("Hotkey register skipped: parse failed.");
            return Task.CompletedTask;
        }

        var handle = new WindowInteropHelper(_window).Handle;
        if (handle == IntPtr.Zero)
        {
            _diagnosticsLogger?.Warning("Hotkey register skipped: window handle unavailable.");
            return Task.CompletedTask;
        }

        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);

        var registered = RegisterHotKey(handle, HotkeyId, modifiers, virtualKey);
        if (!registered)
        {
            _source?.RemoveHook(WndProc);
            _source = null;
            _diagnosticsLogger?.Warning("Hotkey register failed: RegisterHotKey returned false.");
            return Task.CompletedTask;
        }

        _isRegistered = true;
        _diagnosticsLogger?.Info("Hotkey registered.");
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRegistered)
        {
            return Task.CompletedTask;
        }

        var handle = new WindowInteropHelper(_window).Handle;
        if (handle != IntPtr.Zero)
        {
            UnregisterHotKey(handle, HotkeyId);
        }

        _source?.RemoveHook(WndProc);
        _source = null;
        _isRegistered = false;
        _diagnosticsLogger?.Info("Hotkey unregistered.");
        return Task.CompletedTask;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
