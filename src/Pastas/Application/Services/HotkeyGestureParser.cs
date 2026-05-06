using System.Globalization;

namespace Pastas.Application.Services;

public static class HotkeyGestureParser
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    public static bool TryParse(string hotkey, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return false;
        }

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        foreach (var modifier in parts[..^1])
        {
            if (modifier.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModAlt;
                continue;
            }

            if (modifier.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || modifier.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModControl;
                continue;
            }

            if (modifier.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModShift;
                continue;
            }

            if (modifier.Equals("Win", StringComparison.OrdinalIgnoreCase) || modifier.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= ModWin;
                continue;
            }

            return false;
        }

        if (modifiers == 0)
        {
            return false;
        }

        var keyToken = parts[^1];
        if (keyToken.Length != 1)
        {
            return false;
        }

        var key = char.ToUpper(keyToken[0], CultureInfo.InvariantCulture);
        if (key is < 'A' or > 'Z')
        {
            return false;
        }

        virtualKey = key;
        return true;
    }
}
