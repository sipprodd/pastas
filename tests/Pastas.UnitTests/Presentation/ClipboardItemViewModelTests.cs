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
    }

    [Fact]
    public void FromEntity_MasksProtectedPreview()
    {
        var item = new ClipboardItem
        {
            Type = ClipboardItemType.ProtectedText,
            PreviewText = "sensitive",
            ContentText = "sensitive",
            IsProtected = true,
            Hash = "h"
        };

        var vm = ClipboardItemViewModel.FromEntity(item);

        Assert.Equal("********", vm.PreviewText);
        Assert.True(vm.IsProtected);
    }
}
