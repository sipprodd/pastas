using Pastas.Domain.Enums;

namespace Pastas.Application.Services;

public sealed record ClipboardCaptureNotification(ClipboardItemType ItemType, bool IsProtected, int TotalItems)
{
    public string Title => IsProtected || ItemType == ClipboardItemType.ProtectedText
        ? "Protected text copied"
        : ItemType == ClipboardItemType.Image
            ? "Image copied"
            : "Text copied";

    public string Subtitle => $"Total items: {TotalItems}";
}
