using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Pastas.Application.Services;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.Infrastructure.Clipboard;

public sealed class WindowsClipboardGateway : IClipboardGateway
{
    private readonly ClipboardRetryPolicy _retryPolicy;

    public WindowsClipboardGateway(ClipboardRetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy;
    }

    public async Task<ClipboardCaptureData?> ReadAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(
            () => StaClipboardRunner.Run(() =>
            {
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    var text = Clipboard.GetText(TextDataFormat.UnicodeText);
                    if (string.IsNullOrEmpty(text))
                    {
                        return null;
                    }

                    return new ClipboardCaptureData
                    {
                        Type = ClipboardItemType.Text,
                        Text = text,
                        SizeBytes = System.Text.Encoding.UTF8.GetByteCount(text)
                    };
                }

                if (!Clipboard.ContainsImage())
                {
                    return null;
                }

                var bitmap = Clipboard.GetImage();
                if (bitmap is null)
                {
                    return null;
                }

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var stream = new MemoryStream();
                encoder.Save(stream);
                var imageBytes = stream.ToArray();

                return new ClipboardCaptureData
                {
                    Type = ClipboardItemType.Image,
                    ImageBytes = imageBytes,
                    SizeBytes = imageBytes.LongLength
                };
            }),
            cancellationToken);
    }

    public async Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var wrote = await _retryPolicy.ExecuteAsync(
            () => StaClipboardRunner.Run(() => Clipboard.SetText(text, TextDataFormat.UnicodeText)),
            cancellationToken);

        if (!wrote)
        {
            throw new InvalidOperationException("Unable to access clipboard for writing text.");
        }
    }

    public Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Image clipboard write is not supported in this stage.");
    }
}
