using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Presentation.ViewModels;

namespace Pastas.UnitTests.Presentation;

public sealed class ClipboardItemViewModelTests
{
    [Fact]
    public void FromEntity_MapsTextItem()
    {
        var item = new ClipboardItem
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Text,
            PreviewText = "  hello world  ",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal(item.Id, vm.Id);
        Assert.Equal("hello world", vm.PreviewText);
        Assert.True(vm.IsText);
        Assert.False(vm.IsImage);
        Assert.Null(vm.ThumbnailPath);
        Assert.False(vm.HasThumbnail);
    }

    [Fact]
    public void FromEntity_MapsImageThumbnailPath()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Image,
            ThumbnailPath = @"C:\\thumbs\\img.png",
            ImagePath = @"C:\\images\\img.png",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal(item.ThumbnailPath, vm.ThumbnailPath);
        Assert.Equal(item.ImagePath, vm.ImagePath);
    }

    [Fact]
    public void FromEntity_SetsHasThumbnail_TrueWhenImageHasThumbnailPath()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Image,
            ThumbnailPath = @"C:\\thumbs\\img.png",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.True(vm.HasThumbnail);
    }

    [Fact]
    public void FromEntity_SetsHasThumbnail_FalseWhenImageHasNoThumbnailPath()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Screenshot,
            ThumbnailPath = "   ",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.False(vm.HasThumbnail);
        Assert.Null(vm.ThumbnailPath);
    }

    [Fact]
    public void FromEntity_DoesNotUseOriginalImageAsListThumbnail()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Image,
            ImagePath = @"C:\\images\\large-original.png",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.False(vm.HasThumbnail);
        Assert.Null(vm.ThumbnailPath);
        Assert.Equal(item.ImagePath, vm.ImagePath);
        Assert.True(vm.HasImagePath);
    }

    [Fact]
    public void FromEntity_BuildsMetadataLine_WithSourceTimeAndCopyCount()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            PreviewText = "hello",
            SourceApp = "Notepad",
            SourceWindowTitle = "notes.txt - Notepad",
            CopyCount = 3,
            LastCopiedAt = DateTime.Now.AddMinutes(-5),
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal("notes.txt - Notepad", vm.SourceLabel);
        Assert.Equal("Copied 3x", vm.CopyCountLabel);
        Assert.Contains("notes.txt - Notepad", vm.MetadataLine);
        Assert.Contains("Copied 3x", vm.MetadataLine);
    }

    [Fact]
    public void FromEntity_UsesAppFallback_WhenWindowTitleIsMissing()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            PreviewText = "hello",
            SourceApp = "Notepad",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal("Notepad", vm.SourceLabel);
        Assert.Contains("Notepad", vm.MetadataLine);
    }

    [Fact]
    public void FromEntity_UsesUnknownWindowFallback()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            PreviewText = "hello",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal("Unknown window", vm.SourceLabel);
        Assert.Contains("Unknown window", vm.MetadataLine);
    }

    [Fact]
    public void FromEntity_MasksProtectedPreview_AndDoesNotExposeThumbnail()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.ProtectedText,
            PreviewText = "sensitive",
            ContentText = "sensitive",
            IsProtected = true,
            ThumbnailPath = @"C:\\thumbs\\should-not-show.png",
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal("********", vm.PreviewText);
        Assert.True(vm.IsProtected);
        Assert.False(vm.HasThumbnail);
        Assert.Null(vm.ThumbnailPath);
    }
}
