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
    [InlineData("")]
    [InlineData("Ctrl+V")]
    [InlineData("Alt+")]
    [InlineData("Alt+F1")]
    public void TryParse_ReturnsFalse_ForUnsupportedHotkeys(string hotkey)
    {
        var parsed = HotkeyGestureParser.TryParse(hotkey, out _, out _);

        Assert.False(parsed);
    }
}
