namespace Pastas.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default);
    Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
