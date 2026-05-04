using System.IO;
using System.Windows.Media.Imaging;
using WpfClipboard = System.Windows.Clipboard;
using WpfTextDataFormat = System.Windows.TextDataFormat;
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
                if (WpfClipboard.ContainsText(WpfTextDataFormat.UnicodeText))
                {
                    var text = WpfClipboard.GetText(WpfTextDataFormat.UnicodeText);
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

                if (!WpfClipboard.ContainsImage())
                {
                    return null;
                }

                var bitmap = WpfClipboard.GetImage();
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
            () => StaClipboardRunner.Run(() => WpfClipboard.SetText(text, WpfTextDataFormat.UnicodeText)),
            cancellationToken);

        if (!wrote)
        {
            throw new InvalidOperationException("Unable to access clipboard for writing text.");
        }
    }

    public async Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        var wrote = await _retryPolicy.ExecuteAsync(
            () => StaClipboardRunner.Run(() =>
            {
                if (!File.Exists(imagePath))
                {
                    return false;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                WpfClipboard.SetImage(bitmap);
                return true;
            }),
            cancellationToken);

        if (!wrote)
        {
            throw new InvalidOperationException("Unable to access clipboard for writing image.");
        }
    }
}
