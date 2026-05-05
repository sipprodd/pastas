using Pastas.Domain.Entities;
using Pastas.Domain.Interfaces;

namespace Pastas.Application.Services;

public static class StartupSettingsInitializer
{
    public static async Task ApplyAsync(
        ISettingsRepository settingsRepository,
        StartupOptions startupOptions,
        CancellationToken cancellationToken = default)
    {
        if (!StartupOptionsParser.IsSupportedInitialHotkey(startupOptions.InitialHotkey))
        {
            return;
        }

        if (await settingsRepository.HasValueAsync("Hotkey", cancellationToken))
        {
            return;
        }

        var current = await settingsRepository.GetAsync(cancellationToken);
        await settingsRepository.SaveAsync(CopyWithHotkey(current, startupOptions.InitialHotkey!), cancellationToken);
    }

    private static AppSettings CopyWithHotkey(AppSettings settings, string hotkey)
    {
        return new AppSettings
        {
            Hotkey = hotkey,
            MaxItems = settings.MaxItems,
            MaxItemSizeBytes = settings.MaxItemSizeBytes,
            MaxCacheSizeBytes = settings.MaxCacheSizeBytes,
            NotificationsEnabled = settings.NotificationsEnabled,
            CopyStreakEnabled = settings.CopyStreakEnabled,
            ProtectedItemPolicy = settings.ProtectedItemPolicy,
            HideProtectedOnBlur = settings.HideProtectedOnBlur,
            RevealProtectedSeconds = settings.RevealProtectedSeconds,
            ClearProtectedClipboardAfterDelay = settings.ClearProtectedClipboardAfterDelay,
            ClearProtectedClipboardDelaySeconds = settings.ClearProtectedClipboardDelaySeconds,
            ThemeMode = settings.ThemeMode,
            ClearWindowsClipboardHistoryOnStartup = settings.ClearWindowsClipboardHistoryOnStartup
        };
    }
}
