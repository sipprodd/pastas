using Pastas.Application.Services;
using Pastas.Domain.Entities;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.UnitTests.Application;

public sealed class StorageStatsServiceTests
{
    [Fact]
    public async Task GetStatsAsync_UsesRepositoryBackedStats()
    {
        var repository = new FakeClipboardItemRepository
        {
            Stats = new StorageStats
            {
                TotalItems = 4,
                PinnedItems = 2,
                ImageItems = 1,
                ApproxUsageBytes = 1024
            }
        };
        var service = new StorageStatsService(repository);

        var stats = await service.GetStatsAsync();

        Assert.True(repository.GetStorageStatsCalled);
        Assert.False(repository.SearchCalled);
        Assert.Equal(4, stats.TotalItems);
        Assert.Equal(2, stats.PinnedItems);
        Assert.Equal(1, stats.ImageItems);
        Assert.Equal(1024, stats.ApproxUsageBytes);
    }

    [Fact]
    public async Task GetStatsAsync_ClampsNegativeRepositoryValues()
    {
        var repository = new FakeClipboardItemRepository
        {
            Stats = new StorageStats
            {
                TotalItems = -1,
                PinnedItems = -2,
                ImageItems = -3,
                ApproxUsageBytes = -4
            }
        };
        var service = new StorageStatsService(repository);

        var stats = await service.GetStatsAsync();

        Assert.Equal(0, stats.TotalItems);
        Assert.Equal(0, stats.PinnedItems);
        Assert.Equal(0, stats.ImageItems);
        Assert.Equal(0, stats.ApproxUsageBytes);
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public StorageStats Stats { get; init; } = new();
        public bool GetStorageStatsCalled { get; private set; }
        public bool SearchCalled { get; private set; }

        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ClipboardItem>>(Array.Empty<ClipboardItem>());
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
        {
            SearchCalled = true;
            return Task.FromResult<IReadOnlyList<ClipboardItem>>(Array.Empty<ClipboardItem>());
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
        {
            GetStorageStatsCalled = true;
            return Task.FromResult(Stats);
        }
    }
}
