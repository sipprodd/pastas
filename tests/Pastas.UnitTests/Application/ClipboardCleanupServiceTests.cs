using Pastas.Application.Services;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.UnitTests.Application;

public class ClipboardCleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_DeletesOldestNonPinnedItems_WhenOverLimit()
    {
        var repo = CreateRepoWithItems((false, false, null, null), (false, false, null, null), (false, false, null, null));
        var storage = new FakeFileStorage();
        var service = new ClipboardCleanupService(repo, storage, new ClipboardCleanupOptions { MaxItems = 2 });

        await service.CleanupAsync();

        Assert.Equal(2, repo.Items.Count);
        Assert.DoesNotContain(repo.Items, x => x.PreviewText == "item-0");
    }

    [Fact]
    public async Task CleanupAsync_DoesNotDeletePinnedItems()
    {
        var repo = CreateRepoWithItems((true, false, null, null), (false, false, null, null), (false, false, null, null));
        var service = new ClipboardCleanupService(repo, new FakeFileStorage(), new ClipboardCleanupOptions { MaxItems = 2 });

        await service.CleanupAsync();

        Assert.Contains(repo.Items, x => x.IsPinned);
    }

    [Fact]
    public async Task CleanupAsync_DoesNotDeleteProtectedItems()
    {
        var repo = CreateRepoWithItems((false, true, null, null), (false, false, null, null), (false, false, null, null));
        var service = new ClipboardCleanupService(repo, new FakeFileStorage(), new ClipboardCleanupOptions { MaxItems = 2 });

        await service.CleanupAsync();

        Assert.Contains(repo.Items, x => x.IsProtected);
    }

    [Fact]
    public async Task CleanupAsync_DeletesImageAndThumbnailFiles_ForRemovedItems()
    {
        var repo = CreateRepoWithItems((false, false, "image-a", "thumb-a"), (false, false, null, null), (false, false, null, null));
        var storage = new FakeFileStorage();
        var service = new ClipboardCleanupService(repo, storage, new ClipboardCleanupOptions { MaxItems = 2 });

        await service.CleanupAsync();

        Assert.Contains("image-a", storage.DeletedPaths);
        Assert.Contains("thumb-a", storage.DeletedPaths);
    }

    [Fact]
    public async Task CleanupAsync_DoesNothing_WhenWithinLimit()
    {
        var repo = CreateRepoWithItems((false, false, null, null), (false, false, null, null));
        var storage = new FakeFileStorage();
        var service = new ClipboardCleanupService(repo, storage, new ClipboardCleanupOptions { MaxItems = 2 });

        await service.CleanupAsync();

        Assert.Equal(2, repo.Items.Count);
        Assert.Empty(storage.DeletedPaths);
    }

    private static FakeClipboardItemRepository CreateRepoWithItems(params (bool Pinned, bool Protected, string? ImagePath, string? ThumbnailPath)[] flags)
    {
        var repo = new FakeClipboardItemRepository();
        var start = DateTime.UtcNow.AddMinutes(-flags.Length);

        for (var i = 0; i < flags.Length; i++)
        {
            var entry = flags[i];
            repo.Items.Add(new ClipboardItem
            {
                Id = Guid.NewGuid(),
                Type = entry.ImagePath is null ? ClipboardItemType.Text : ClipboardItemType.Image,
                PreviewText = $"item-{i}",
                IsPinned = entry.Pinned,
                IsProtected = entry.Protected,
                ImagePath = entry.ImagePath,
                ThumbnailPath = entry.ThumbnailPath,
                CreatedAt = start.AddMinutes(i),
                LastCopiedAt = start.AddMinutes(i),
                UpdatedAt = start.AddMinutes(i)
            });
        }

        return repo;
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public List<ClipboardItem> Items { get; } = [];
        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Hash == hash));
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ClipboardItem>)Items.OrderBy(x => x.LastCopiedAt).ToList());
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.Count);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public List<string> DeletedPaths { get; } = [];
        public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            DeletedPaths.Add(path);
            return Task.CompletedTask;
        }
    }
}
