using Pastas.Domain.Entities;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.Presentation.Design;

public sealed class EmptyClipboardItemRepository : IClipboardItemRepository
{
    public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
    public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
    public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClipboardItem>>(Array.Empty<ClipboardItem>());
    public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClipboardItem>>(Array.Empty<ClipboardItem>());
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
