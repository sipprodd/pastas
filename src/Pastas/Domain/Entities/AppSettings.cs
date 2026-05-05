using Pastas.Domain.Enums;

namespace Pastas.Domain.Entities;

public sealed class AppSettings
{
    public string Hotkey { get; init; } = "Alt+V";
    public int MaxItems { get; init; } = 200;
    public long MaxItemSizeBytes { get; init; } = 25 * 1024 * 1024;
    public long MaxCacheSizeBytes { get; init; } = 256 * 1024 * 1024;
    public bool NotificationsEnabled { get; init; } = true;
    public bool CopyStreakEnabled { get; init; } = true;
    public ProtectedItemPolicy ProtectedItemPolicy { get; init; } = ProtectedItemPolicy.SaveEncrypted;
    public bool HideProtectedOnBlur { get; init; } = true;
    public int RevealProtectedSeconds { get; init; } = 10;
    public bool ClearProtectedClipboardAfterDelay { get; init; } = false;
    public int ClearProtectedClipboardDelaySeconds { get; init; } = 60;
    public ThemeMode ThemeMode { get; init; } = ThemeMode.Chocolate;
    public bool ClearWindowsClipboardHistoryOnStartup { get; init; } = false;
}
