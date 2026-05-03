using Microsoft.Data.Sqlite;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;

namespace Pastas.Infrastructure.Storage.SQLite;

public sealed class SqliteSettingsRepository : ISettingsRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteSettingsRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM app_settings;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values[reader.GetString(0)] = reader.GetString(1);
        }

        var defaults = new AppSettings();
        return new AppSettings
        {
            Hotkey = GetValue(values, "Hotkey", defaults.Hotkey),
            MaxItems = GetInt(values, "MaxItems", defaults.MaxItems),
            MaxItemSizeBytes = GetLong(values, "MaxItemSizeBytes", defaults.MaxItemSizeBytes),
            MaxCacheSizeBytes = GetLong(values, "MaxCacheSizeBytes", defaults.MaxCacheSizeBytes),
            NotificationsEnabled = GetBool(values, "NotificationsEnabled", defaults.NotificationsEnabled),
            CopyStreakEnabled = GetBool(values, "CopyStreakEnabled", defaults.CopyStreakEnabled),
            ProtectedItemPolicy = GetEnum(values, "ProtectedItemPolicy", defaults.ProtectedItemPolicy),
            HideProtectedOnBlur = GetBool(values, "HideProtectedOnBlur", defaults.HideProtectedOnBlur),
            RevealProtectedSeconds = GetInt(values, "RevealProtectedSeconds", defaults.RevealProtectedSeconds),
            ClearProtectedClipboardAfterDelay = GetBool(values, "ClearProtectedClipboardAfterDelay", defaults.ClearProtectedClipboardAfterDelay),
            ClearProtectedClipboardDelaySeconds = GetInt(values, "ClearProtectedClipboardDelaySeconds", defaults.ClearProtectedClipboardDelaySeconds),
            ThemeMode = GetEnum(values, "ThemeMode", defaults.ThemeMode)
        };
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var values = new Dictionary<string, string>
        {
            ["Hotkey"] = settings.Hotkey,
            ["MaxItems"] = settings.MaxItems.ToString(),
            ["MaxItemSizeBytes"] = settings.MaxItemSizeBytes.ToString(),
            ["MaxCacheSizeBytes"] = settings.MaxCacheSizeBytes.ToString(),
            ["NotificationsEnabled"] = settings.NotificationsEnabled.ToString(),
            ["CopyStreakEnabled"] = settings.CopyStreakEnabled.ToString(),
            ["ProtectedItemPolicy"] = settings.ProtectedItemPolicy.ToString(),
            ["HideProtectedOnBlur"] = settings.HideProtectedOnBlur.ToString(),
            ["RevealProtectedSeconds"] = settings.RevealProtectedSeconds.ToString(),
            ["ClearProtectedClipboardAfterDelay"] = settings.ClearProtectedClipboardAfterDelay.ToString(),
            ["ClearProtectedClipboardDelaySeconds"] = settings.ClearProtectedClipboardDelaySeconds.ToString(),
            ["ThemeMode"] = settings.ThemeMode.ToString()
        };

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var entry in values)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO app_settings(key, value) VALUES(@key, @value) ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
            command.Parameters.AddWithValue("@key", entry.Key);
            command.Parameters.AddWithValue("@value", entry.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, string key, string defaultValue)
        => values.TryGetValue(key, out var value) ? value : defaultValue;

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int defaultValue)
        => values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static long GetLong(IReadOnlyDictionary<string, string> values, string key, long defaultValue)
        => values.TryGetValue(key, out var value) && long.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool GetBool(IReadOnlyDictionary<string, string> values, string key, bool defaultValue)
        => values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static TEnum GetEnum<TEnum>(IReadOnlyDictionary<string, string> values, string key, TEnum defaultValue)
        where TEnum : struct
        => values.TryGetValue(key, out var value) && Enum.TryParse<TEnum>(value, out var parsed) ? parsed : defaultValue;
}
