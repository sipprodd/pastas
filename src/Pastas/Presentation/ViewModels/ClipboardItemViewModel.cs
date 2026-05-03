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
    public bool IsPinned { get; private init; }
    public bool IsProtected { get; private init; }
    public bool IsImage { get; private init; }
    public bool IsText { get; private init; }
    public string? ThumbnailPath { get; private init; }
    public string? ImagePath { get; private init; }
    public string FullText { get; private init; } = string.Empty;
    public bool HasThumbnail { get; private init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string ItemGlyph => IsProtected ? "🔒" : IsImage ? "🖼" : "T";

    public static ClipboardItemViewModel FromEntity(ClipboardItem item)
    {
        var isImage = item.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot;
        var isProtected = item.IsProtected || item.Type == ClipboardItemType.ProtectedText;
        var thumbnailPath = isImage && !string.IsNullOrWhiteSpace(item.ThumbnailPath)
            ? item.ThumbnailPath
            : null;

        return new ClipboardItemViewModel
        {
            Id = item.Id,
            Title = isImage ? "Image item" : "Text item",
            PreviewText = BuildPreview(item, isImage, isProtected),
            TypeLabel = isImage ? "Image" : "Text",
            SourceLabel = BuildSourceLabel(item),
            TimeLabel = item.LastCopiedAt.ToLocalTime().ToString("g"),
            IsPinned = item.IsPinned,
            IsProtected = isProtected,
            IsImage = isImage,
            IsText = !isImage,
            ThumbnailPath = thumbnailPath,
            ImagePath = isImage ? item.ImagePath : null,
            FullText = item.ContentText ?? string.Empty,
            HasThumbnail = !string.IsNullOrWhiteSpace(thumbnailPath)
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
            return "Image item";
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
        if (!string.IsNullOrWhiteSpace(item.SourceApp))
        {
            return item.SourceApp.Trim();
        }

        if (!string.IsNullOrWhiteSpace(item.SourceWindowTitle))
        {
            return item.SourceWindowTitle.Trim();
        }

        return "Unknown source";
    }
}
