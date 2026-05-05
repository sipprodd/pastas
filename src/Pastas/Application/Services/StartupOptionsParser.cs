namespace Pastas.Application.Services;

public static class StartupOptionsParser
{
    private const string InitialHotkeyPrefix = "--initial-hotkey=";
    private const string StartMinimizedSwitch = "--start-minimized";

    private static readonly HashSet<string> SupportedInitialHotkeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alt+V",
        "Ctrl+Alt+V",
        "Ctrl+Shift+V"
    };

    public static StartupOptions Parse(IEnumerable<string> args)
    {
        string? initialHotkey = null;
        var startMinimized = false;

        foreach (var arg in args)
        {
            if (arg.Equals(StartMinimizedSwitch, StringComparison.OrdinalIgnoreCase))
            {
                startMinimized = true;
                continue;
            }

            if (!arg.StartsWith(InitialHotkeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = arg[InitialHotkeyPrefix.Length..].Trim();
            if (IsSupportedInitialHotkey(value))
            {
                initialHotkey = NormalizeHotkey(value);
            }
        }

        return new StartupOptions
        {
            InitialHotkey = initialHotkey,
            StartMinimized = startMinimized
        };
    }

    public static bool IsSupportedInitialHotkey(string? hotkey)
        => !string.IsNullOrWhiteSpace(hotkey)
           && SupportedInitialHotkeys.Contains(hotkey.Trim())
           && HotkeyGestureParser.TryParse(hotkey.Trim(), out _, out _);

    private static string NormalizeHotkey(string hotkey)
        => SupportedInitialHotkeys.First(x => x.Equals(hotkey, StringComparison.OrdinalIgnoreCase));
}
