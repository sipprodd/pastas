using Pastas.Domain.Enums;

namespace Pastas.Domain.ValueObjects;

public sealed class ClipboardCaptureData
{
    public ClipboardItemType Type { get; init; } = ClipboardItemType.Text;
    public string? Text { get; init; }
    public byte[]? ImageBytes { get; init; }
    public string? SourceApp { get; init; }
    public string? SourceWindowTitle { get; init; }
    public long SizeBytes { get; init; }
}
