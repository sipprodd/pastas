using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.Application.Services;

public sealed class ClipboardCleanupService
{
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ClipboardCleanupOptions _options;
    private readonly IDiagnosticsLogger? _diagnosticsLogger;

    public ClipboardCleanupService(
        IClipboardItemRepository clipboardItemRepository,
        IFileStorage fileStorage,
        ClipboardCleanupOptions options,
        IDiagnosticsLogger? diagnosticsLogger = null)
    {
        _clipboardItemRepository = clipboardItemRepository;
        _fileStorage = fileStorage;
        _options = options;
        _diagnosticsLogger = diagnosticsLogger;
    }

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => CleanupAsync(_options.MaxItems, cancellationToken);

    public async Task CleanupAsync(int maxItems, CancellationToken cancellationToken = default)
    {
        if (maxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Max items must be positive.");
        }

        try
        {
            var totalCount = await _clipboardItemRepository.CountAsync(cancellationToken);
            if (totalCount <= maxItems)
            {
                return;
            }

            var oldestFirst = await _clipboardItemRepository.SearchAsync(
                new ClipboardSearchQuery { SortMode = SortMode.Oldest, Limit = totalCount },
                cancellationToken);
            var candidates = oldestFirst
                .OrderBy(x => x.LastCopiedAt)
                .ThenBy(x => x.CreatedAt)
                .Where(x => !x.IsPinned)
                .ToList();

            var remainingToDelete = totalCount - maxItems;
            var deletedCount = 0;

            // If pinned items alone exceed the max, preserve them and allow the total to remain above the limit.
            foreach (var item in candidates)
            {
                if (remainingToDelete <= 0)
                {
                    break;
                }

                await SafeDeleteFileAsync(item.ImagePath, cancellationToken);
                await SafeDeleteFileAsync(item.ThumbnailPath, cancellationToken);
                await _clipboardItemRepository.DeleteAsync(item.Id, cancellationToken);
                remainingToDelete--;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                _diagnosticsLogger?.Info($"Cleanup removed items. Count={deletedCount}.");
            }
        }
        catch (Exception ex)
        {
            _diagnosticsLogger?.Error("Cleanup failed.", ex);
            throw;
        }
    }

    private async Task SafeDeleteFileAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            await _fileStorage.DeleteAsync(path, cancellationToken);
        }
        catch
        {
        }
    }
}
