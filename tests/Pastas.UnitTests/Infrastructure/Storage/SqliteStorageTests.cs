using Microsoft.Data.Sqlite;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.ValueObjects;
using Pastas.Infrastructure.Storage.SQLite;

namespace Pastas.UnitTests.Infrastructure.Storage;

public sealed class SqliteStorageTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _dbPath;
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteStorageTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "pastas-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _dbPath = Path.Combine(_tempDirectory, "pastas-test.db");
        _connectionFactory = new SqliteConnectionFactory(_dbPath);

        var migrationRunner = new SqliteMigrationRunner(_connectionFactory);
        migrationRunner.RunAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task MigrationRunner_CreatesRequiredTables_AndIsIdempotent()
    {
        var migrationRunner = new SqliteMigrationRunner(_connectionFactory);
        await migrationRunner.RunAsync();

        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('schema_version', 'clipboard_items', 'app_settings') ORDER BY name;";

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Equal(new[] { "app_settings", "clipboard_items", "schema_version" }, tables);
    }

    [Fact]
    public async Task ClipboardRepository_AddAndGet_Works()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var item = BuildTextItem("hash-a");

        await repository.AddAsync(item);
        var saved = await repository.GetByIdAsync(item.Id);

        Assert.NotNull(saved);
        Assert.Equal(item.Hash, saved!.Hash);
    }

    [Fact]
    public async Task ClipboardRepository_FindByHash_Works()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var item = BuildTextItem("hash-b");
        await repository.AddAsync(item);

        var found = await repository.FindByHashAsync("hash-b");

        Assert.NotNull(found);
        Assert.Equal(item.Id, found!.Id);
    }

    [Fact]
    public async Task ClipboardRepository_Search_FilterText_Works()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        await repository.AddAsync(BuildTextItem("hash-c"));
        await repository.AddAsync(BuildImageItem("hash-d"));

        var results = await repository.SearchAsync(new ClipboardSearchQuery { Filter = ClipboardFilter.Text });

        Assert.Single(results);
        Assert.Equal(ClipboardItemType.Text, results[0].Type);
    }

    [Fact]
    public async Task ClipboardRepository_Search_ExcludesProtectedContentFieldFromQuery()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var baseItem = BuildTextItem("hash-e");
        var protectedItem = new ClipboardItem
        {
            Id = baseItem.Id,
            Type = baseItem.Type,
            PreviewText = string.Empty,
            ContentText = baseItem.ContentText,
            EncryptedContent = "secret-token",
            Hash = baseItem.Hash,
            IsProtected = true,
            CreatedAt = baseItem.CreatedAt,
            UpdatedAt = baseItem.UpdatedAt,
            LastCopiedAt = baseItem.LastCopiedAt
        };
        await repository.AddAsync(protectedItem);

        var results = await repository.SearchAsync(new ClipboardSearchQuery { Query = "secret-token" });

        Assert.Empty(results);
    }

    [Fact]
    public async Task ClipboardRepository_Update_Works()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var item = BuildTextItem("hash-f");
        await repository.AddAsync(item);

        var updated = new ClipboardItem
        {
            Id = item.Id,
            Type = item.Type,
            PreviewText = "updated",
            ContentText = item.ContentText,
            Hash = item.Hash,
            CopyCount = 9,
            CreatedAt = item.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            LastCopiedAt = item.LastCopiedAt
        };
        await repository.UpdateAsync(updated);

        var saved = await repository.GetByIdAsync(item.Id);
        Assert.Equal("updated", saved!.PreviewText);
        Assert.Equal(9, saved.CopyCount);
    }

    [Fact]
    public async Task ClipboardRepository_Delete_Works()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var item = BuildTextItem("hash-g");
        await repository.AddAsync(item);

        await repository.DeleteAsync(item.Id);
        var deleted = await repository.GetByIdAsync(item.Id);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task ClipboardRepository_DeleteByCategories_AllFalse_DeletesNothing()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        await repository.AddAsync(BuildTextItem("hash-keep-1"));

        var deleted = await repository.DeleteByCategoriesAsync(false, false, false);

        Assert.Empty(deleted);
        Assert.Equal(1, await repository.CountAsync());
    }

    [Fact]
    public async Task ClipboardRepository_DeleteByCategories_RespectsPinnedAndTypeRules()
    {
        var repository = new SqliteClipboardItemRepository(_connectionFactory);
        var text = BuildTextItem("hash-t");
        var pinnedText = BuildTextItem("hash-pt", true);
        var image = BuildImageItem("hash-i");
        var pinnedImage = BuildImageItem("hash-pi", true);
        await repository.AddAsync(text);
        await repository.AddAsync(pinnedText);
        await repository.AddAsync(image);
        await repository.AddAsync(pinnedImage);

        var deletedTextOnly = await repository.DeleteByCategoriesAsync(true, false, false);
        Assert.Single(deletedTextOnly);
        Assert.Equal(text.Id, deletedTextOnly[0].Id);

        var deletedImagesOnly = await repository.DeleteByCategoriesAsync(false, true, false);
        Assert.Single(deletedImagesOnly);
        Assert.Equal(image.Id, deletedImagesOnly[0].Id);

        var deletedPinned = await repository.DeleteByCategoriesAsync(false, false, true);
        Assert.Equal(2, deletedPinned.Count);
    }

    [Fact]
    public async Task SettingsRepository_ReturnsDefaults_WhenEmpty()
    {
        var repository = new SqliteSettingsRepository(_connectionFactory);

        var settings = await repository.GetAsync();

        Assert.Equal(new AppSettings().Hotkey, settings.Hotkey);
        Assert.Equal(new AppSettings().ThemeMode, settings.ThemeMode);
    }

    [Fact]
    public async Task SettingsRepository_SavesAndLoads()
    {
        var repository = new SqliteSettingsRepository(_connectionFactory);
        var settings = new AppSettings
        {
            Hotkey = "Ctrl+Shift+V",
            MaxItems = 999,
            ThemeMode = ThemeMode.Black,
            NotificationsEnabled = false
        };

        await repository.SaveAsync(settings);
        var loaded = await repository.GetAsync();

        Assert.Equal("Ctrl+Shift+V", loaded.Hotkey);
        Assert.Equal(999, loaded.MaxItems);
        Assert.Equal(ThemeMode.Black, loaded.ThemeMode);
        Assert.False(loaded.NotificationsEnabled);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup only. Avoid failing tests on transient file locks.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup only. Avoid failing tests on transient file locks.
            }
        }
    }

    private static ClipboardItem BuildTextItem(string hash, bool isPinned = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Text,
            PreviewText = "hello world",
            ContentText = "hello world content",
            Hash = hash,
            IsPinned = isPinned,
            LastCopiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static ClipboardItem BuildImageItem(string hash, bool isPinned = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Image,
            ImagePath = "image.png",
            Hash = hash,
            IsPinned = isPinned,
            LastCopiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
}
