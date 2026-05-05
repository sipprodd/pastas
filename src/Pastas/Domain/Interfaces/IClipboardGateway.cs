using Pastas.Domain.ValueObjects;

namespace Pastas.Domain.Interfaces;

public interface IClipboardGateway
{
    Task<ClipboardCaptureData?> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteTextAsync(string text, CancellationToken cancellationToken = default);
    Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default);
    Task WriteImageBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
