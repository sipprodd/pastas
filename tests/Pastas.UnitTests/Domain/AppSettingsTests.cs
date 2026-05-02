using Pastas.Domain.Entities;
using Pastas.Domain.Enums;

namespace Pastas.UnitTests.Domain;

public class AppSettingsTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var settings = new AppSettings();

        Assert.Equal("Alt+V", settings.Hotkey);
        Assert.Equal(200, settings.MaxItems);
        Assert.Equal(25 * 1024 * 1024, settings.MaxItemSizeBytes);
        Assert.Equal(256 * 1024 * 1024, settings.MaxCacheSizeBytes);
        Assert.True(settings.NotificationsEnabled);
        Assert.True(settings.CopyStreakEnabled);
        Assert.Equal(ProtectedItemPolicy.SaveEncrypted, settings.ProtectedItemPolicy);
        Assert.True(settings.HideProtectedOnBlur);
        Assert.Equal(10, settings.RevealProtectedSeconds);
        Assert.False(settings.ClearProtectedClipboardAfterDelay);
        Assert.Equal(60, settings.ClearProtectedClipboardDelaySeconds);
        Assert.Equal(ThemeMode.System, settings.ThemeMode);
    }
}
