using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pastas.Application.Services;

namespace Pastas.Infrastructure.Files;

public sealed class ImageThumbnailBuilder : IImageThumbnailBuilder
{
    private const int MaxSize = 256;

    public Task<byte[]> BuildAsync(byte[] originalImageBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(originalImageBytes);

        using var input = new MemoryStream(originalImageBytes);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var scale = Math.Min(1.0, Math.Min((double)MaxSize / frame.PixelWidth, (double)MaxSize / frame.PixelHeight));
        var width = Math.Max(1, (int)Math.Round(frame.PixelWidth * scale));
        var height = Math.Max(1, (int)Math.Round(frame.PixelHeight * scale));

        BitmapSource bitmapSource = frame;
        if (scale < 1.0)
        {
            bitmapSource = new TransformedBitmap(frame, new ScaleTransform(scale, scale));
            bitmapSource.Freeze();
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using var output = new MemoryStream();
        encoder.Save(output);

        return Task.FromResult(output.ToArray());
    }
}
