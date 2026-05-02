using Pastas.Domain.Entities;
using Pastas.Domain.Enums;

namespace Pastas.UnitTests.Domain;

public class ClipboardItemTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var before = DateTime.UtcNow;
        var item = new ClipboardItem();
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(ClipboardItemType.Text, item.Type);
        Assert.Null(item.PreviewText);
        Assert.Null(item.ContentText);
        Assert.Null(item.EncryptedContent);
        Assert.Null(item.ImagePath);
        Assert.Null(item.ThumbnailPath);
        Assert.Null(item.SourceApp);
        Assert.Null(item.SourceWindowTitle);
        Assert.Equal(string.Empty, item.Hash);
        Assert.False(item.IsPinned);
        Assert.False(item.IsProtected);
        Assert.Equal(1, item.CopyCount);
        Assert.Equal(0, item.SizeBytes);
        Assert.InRange(item.CreatedAt, before, after);
        Assert.InRange(item.UpdatedAt, before, after);
        Assert.InRange(item.LastCopiedAt, before, after);
    }
}
