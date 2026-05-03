using System.IO;
using Pastas.Domain.Interfaces;

namespace Pastas.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _imagesPath;
    private readonly string _thumbnailsPath;

    public LocalFileStorage()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pastas"))
    {
    }

    public LocalFileStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _imagesPath = Path.Combine(rootPath, "images");
        _thumbnailsPath = Path.Combine(rootPath, "thumbnails");
    }

    public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default)
        => SaveAsync(bytes, _imagesPath, itemId, cancellationToken);

    public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default)
        => SaveAsync(bytes, _thumbnailsPath, itemId, cancellationToken);

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static async Task<string> SaveAsync(byte[] bytes, string directoryPath, Guid itemId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        Directory.CreateDirectory(directoryPath);
        var fullPath = Path.Combine(directoryPath, $"{itemId}.png");
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        return fullPath;
    }
}
