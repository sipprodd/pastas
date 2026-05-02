using System.Globalization;

namespace Pastas.Application.Services;

public static class HotkeyGestureParser
{
    private const uint ModAlt = 0x0001;

    public static bool TryParse(string hotkey, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return false;
        }

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!parts[0].Equals("Alt", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (parts[1].Length != 1)
        {
            return false;
        }

        var key = char.ToUpper(parts[1][0], CultureInfo.InvariantCulture);
        if (key is < 'A' or > 'Z')
        {
            return false;
        }

        modifiers = ModAlt;
        virtualKey = key;
        return true;
    }
}
