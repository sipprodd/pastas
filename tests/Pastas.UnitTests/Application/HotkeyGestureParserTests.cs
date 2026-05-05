using Pastas.Application.Services;

namespace Pastas.UnitTests.Application;

public class HotkeyGestureParserTests
{
    [Fact]
    public void TryParse_ReturnsTrue_ForAltPlusV()
    {
        var parsed = HotkeyGestureParser.TryParse("Alt+V", out var modifiers, out var virtualKey);

        Assert.True(parsed);
        Assert.Equal(0x0001u, modifiers);
        Assert.Equal((uint)'V', virtualKey);
    }

    [Theory]
    [InlineData("Ctrl+Shift+V", 0x0006u)]
    [InlineData("Win+V", 0x0008u)]
    public void TryParse_ReturnsTrue_ForSupportedHotkeys(string hotkey, uint expectedModifiers)
    {
        var parsed = HotkeyGestureParser.TryParse(hotkey, out var modifiers, out var virtualKey);

        Assert.True(parsed);
        Assert.Equal(expectedModifiers, modifiers);
        Assert.Equal((uint)'V', virtualKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("V")]
    [InlineData("Alt+")]
    [InlineData("Alt+F1")]
    public void TryParse_ReturnsFalse_ForUnsupportedHotkeys(string hotkey)
    {
        var parsed = HotkeyGestureParser.TryParse(hotkey, out _, out _);

        Assert.False(parsed);
    }
}
