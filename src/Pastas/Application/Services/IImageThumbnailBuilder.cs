namespace Pastas.Application.Services;

public interface IImageThumbnailBuilder
{
    Task<byte[]> BuildAsync(byte[] originalImageBytes, CancellationToken cancellationToken = default);
}
