using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
                var source = ReadForegroundSource();

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
                        SizeBytes = Encoding.UTF8.GetByteCount(text),
                        SourceApp = source.App,
                        SourceWindowTitle = source.WindowTitle
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
                    SizeBytes = imageBytes.LongLength,
                    SourceApp = source.App,
                    SourceWindowTitle = source.WindowTitle
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

    public async Task WriteImageBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        var wrote = await _retryPolicy.ExecuteAsync(
            () => StaClipboardRunner.Run(() =>
            {
                if (imageBytes.Length == 0)
                {
                    return false;
                }

                using var stream = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
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

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var cleared = await _retryPolicy.ExecuteAsync(
            () => StaClipboardRunner.Run(() =>
            {
                WpfClipboard.Clear();
                return true;
            }),
            cancellationToken);

        if (!cleared)
        {
            throw new InvalidOperationException("Unable to access clipboard for clearing.");
        }
    }

    private static (string? App, string? WindowTitle) ReadForegroundSource()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return (null, null);
        }

        var titleBuilder = new StringBuilder(512);
        var titleLength = GetWindowText(handle, titleBuilder, titleBuilder.Capacity);
        var title = titleLength > 0 ? titleBuilder.ToString().Trim() : null;

        string? app = null;
        try
        {
            _ = GetWindowThreadProcessId(handle, out var processId);
            if (processId != 0)
            {
                using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                app = process.ProcessName;
            }
        }
        catch
        {
            app = null;
        }

        return (string.IsNullOrWhiteSpace(app) ? null : app, string.IsNullOrWhiteSpace(title) ? null : title);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
