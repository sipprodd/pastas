using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.Application.Services;

public sealed class StorageStatsService
{
    private readonly IClipboardItemRepository _clipboardItemRepository;

    public StorageStatsService(IClipboardItemRepository clipboardItemRepository)
    {
        _clipboardItemRepository = clipboardItemRepository;
    }

    public async Task<StorageStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = await _clipboardItemRepository.GetStorageStatsAsync(cancellationToken);
        return new StorageStats
        {
            TotalItems = Math.Max(0, stats.TotalItems),
            PinnedItems = Math.Max(0, stats.PinnedItems),
            ImageItems = Math.Max(0, stats.ImageItems),
            ApproxUsageBytes = Math.Max(0, stats.ApproxUsageBytes)
        };
    }
}
