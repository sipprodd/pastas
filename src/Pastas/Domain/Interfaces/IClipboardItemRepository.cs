using Pastas.Domain.Entities;
using Pastas.Domain.ValueObjects;

namespace Pastas.Domain.Interfaces;

public interface IClipboardItemRepository
{
    Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default);
}
