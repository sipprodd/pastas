using Pastas.Domain.Entities;
using Pastas.Domain.Enums;

namespace Pastas.Presentation.ViewModels;

public sealed class ClipboardItemViewModel : ViewModelBase
{
    private bool _isSelected;

    private ClipboardItemViewModel()
    {
    }

    public Guid Id { get; private init; }
    public string Title { get; private init; } = string.Empty;
    public string PreviewText { get; private init; } = string.Empty;
    public string TypeLabel { get; private init; } = string.Empty;
    public string SourceLabel { get; private init; } = string.Empty;
    public string TimeLabel { get; private init; } = string.Empty;
    public string CopyCountLabel { get; private init; } = string.Empty;
    public string MetadataLine { get; private init; } = string.Empty;
    public bool IsPinned { get; private init; }
    public bool IsProtected { get; private init; }
    public bool IsImage { get; private init; }
    public bool IsText { get; private init; }
    public string? ThumbnailPath { get; private init; }
    public string? ImagePath { get; private init; }
    public string FullText { get; private init; } = string.Empty;
    public bool HasThumbnail { get; private init; }
    public bool HasImagePath { get; private init; }

    public string PreviewBodyText => string.IsNullOrWhiteSpace(FullText) ? PreviewText : FullText;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public static ClipboardItemViewModel FromEntity(ClipboardItem item)
    {
        var isImage = item.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot;
        var isProtected = item.IsProtected || item.Type == ClipboardItemType.ProtectedText;
        var thumbnailPath = isImage && !string.IsNullOrWhiteSpace(item.ThumbnailPath)
            ? item.ThumbnailPath
            : null;
        var sourceLabel = BuildSourceLabel(item);
        var timeLabel = BuildTimeLabel(item.LastCopiedAt);
        var copyCountLabel = BuildCopyCountLabel(item.CopyCount);

        return new ClipboardItemViewModel
        {
            Id = item.Id,
            Title = isImage ? "Image item" : "Text item",
            PreviewText = BuildPreview(item, isImage, isProtected),
            TypeLabel = isImage ? "Image" : "Text",
            SourceLabel = sourceLabel,
            TimeLabel = timeLabel,
            CopyCountLabel = copyCountLabel,
            MetadataLine = $"{sourceLabel} · {timeLabel} · {copyCountLabel}",
            IsPinned = item.IsPinned,
            IsProtected = isProtected,
            IsImage = isImage,
            IsText = !isImage,
            ThumbnailPath = thumbnailPath,
            ImagePath = isImage ? item.ImagePath : null,
            FullText = item.ContentText ?? string.Empty,
            HasThumbnail = !string.IsNullOrWhiteSpace(thumbnailPath),
            HasImagePath = isImage && !string.IsNullOrWhiteSpace(item.ImagePath)
        };
    }

    private static string BuildPreview(ClipboardItem item, bool isImage, bool isProtected)
    {
        if (isProtected)
        {
            return "********";
        }

        if (isImage)
        {
            return string.IsNullOrWhiteSpace(item.ImagePath) ? "Image unavailable" : "Image item";
        }

        var text = string.IsNullOrWhiteSpace(item.PreviewText) ? item.ContentText : item.PreviewText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(empty)";
        }

        var trimmed = text.Trim();
        return trimmed.Length <= 140 ? trimmed : $"{trimmed[..140]}...";
    }

    private static string BuildSourceLabel(ClipboardItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.SourceWindowTitle))
        {
            return item.SourceWindowTitle.Trim();
        }

        if (!string.IsNullOrWhiteSpace(item.SourceApp))
        {
            return item.SourceApp.Trim();
        }

        return "Unknown window";
    }

    private static string BuildTimeLabel(DateTime lastCopiedAt)
    {
        var local = lastCopiedAt == default ? DateTime.Now : lastCopiedAt.ToLocalTime();
        var now = DateTime.Now;
        var elapsed = now - local;

        if (elapsed.TotalSeconds >= 0 && elapsed.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (elapsed.TotalMinutes >= 0 && elapsed.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Floor(elapsed.TotalMinutes));
            return $"{minutes}m ago";
        }

        if (elapsed.TotalHours >= 0 && elapsed.TotalHours < 24)
        {
            var hours = Math.Max(1, (int)Math.Floor(elapsed.TotalHours));
            return $"{hours}h ago";
        }

        if (local.Date == now.Date.AddDays(-1))
        {
            return $"Yesterday {local:HH:mm}";
        }

        return local.ToString("MMM d, HH:mm");
    }

    private static string BuildCopyCountLabel(int copyCount)
        => $"Copied {Math.Max(1, copyCount)}x";
}
