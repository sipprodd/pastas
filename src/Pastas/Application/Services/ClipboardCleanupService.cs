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

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _diagnosticsLogger?.Info("Cleanup started.");

        try
        {
            var totalCount = await _clipboardItemRepository.CountAsync(cancellationToken);
            if (totalCount <= _options.MaxItems)
            {
                _diagnosticsLogger?.Info("Cleanup skipped: within max item limit.");
                return;
            }

            var oldestFirst = await _clipboardItemRepository.SearchAsync(
                new ClipboardSearchQuery { SortMode = SortMode.Oldest, Limit = totalCount },
                cancellationToken);
            var candidates = oldestFirst
                .OrderBy(x => x.LastCopiedAt)
                .ThenBy(x => x.CreatedAt)
                .Where(x => !x.IsPinned && !x.IsProtected)
                .ToList();

            var remainingToDelete = totalCount - _options.MaxItems;

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
            }

            _diagnosticsLogger?.Info("Cleanup completed.");
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
