using Pastas.Domain.Enums;

namespace Pastas.Domain.Entities;

public sealed class ClipboardItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ClipboardItemType Type { get; init; } = ClipboardItemType.Text;
    public string? PreviewText { get; init; }
    public string? ContentText { get; init; }
    public string? EncryptedContent { get; init; }
    public string? ImagePath { get; init; }
    public string? ThumbnailPath { get; init; }
    public string? SourceApp { get; init; }
    public string? SourceWindowTitle { get; init; }
    public string Hash { get; init; } = string.Empty;
    public bool IsPinned { get; init; }
    public bool IsProtected { get; init; }
    public int CopyCount { get; init; } = 1;
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastCopiedAt { get; init; } = DateTime.UtcNow;
}
