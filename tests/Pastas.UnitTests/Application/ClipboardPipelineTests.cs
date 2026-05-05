using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.UnitTests.Application;

public class ClipboardPipelineTests
{
    [Fact]
    public void ClipboardTextHasher_ProducesStableHash_ForSameText()
    {
        var hash1 = ClipboardTextHasher.Compute("hello world");
        var hash2 = ClipboardTextHasher.Compute("hello world");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ClipboardTextHasher_ProducesDifferentHashes_ForDifferentText()
    {
        var hash1 = ClipboardTextHasher.Compute("hello world");
        var hash2 = ClipboardTextHasher.Compute("hello world!");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void TextPreviewBuilder_KeepsShortText()
    {
        var text = "hello";

        var preview = TextPreviewBuilder.Build(text);

        Assert.Equal(text, preview);
    }

    [Fact]
    public void TextPreviewBuilder_TruncatesLongText_ToMax300Characters()
    {
        var text = new string('a', 350);

        var preview = TextPreviewBuilder.Build(text);

        Assert.Equal(300, preview.Length);
        Assert.Equal(new string('a', 300), preview);
    }

    [Fact]
    public void ClipboardCaptureState_ConsumesInternalWriteFlag_Once()
    {
        var state = new ClipboardCaptureState();
        state.MarkInternalClipboardWrite();

        Assert.True(state.ConsumeInternalClipboardWriteFlag());
        Assert.False(state.ConsumeInternalClipboardWriteFlag());
    }

    [Fact]
    public void ClipboardCaptureState_ConsumesDoNotSaveNextFlag_Once()
    {
        var state = new ClipboardCaptureState();
        state.MarkDoNotSaveNextCapture();

        Assert.True(state.ConsumeDoNotSaveNextFlag());
        Assert.False(state.ConsumeDoNotSaveNextFlag());
    }

    [Fact]
    public void ClipboardCaptureState_TogglesCapturePause()
    {
        var state = new ClipboardCaptureState();

        Assert.False(state.IsCapturePaused);
        Assert.True(state.ToggleCapturePause());
        Assert.True(state.IsCapturePaused);

        state.ResumeCapture();
        Assert.False(state.IsCapturePaused);

        state.PauseCapture();
        Assert.True(state.IsCapturePaused);
    }

    [Fact]
    public async Task CaptureClipboardTextUseCase_AddsNewTextItem()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Text = "sample" } };
        var repo = new FakeClipboardItemRepository();
        var state = new ClipboardCaptureState();
        var useCase = new CaptureClipboardTextUseCase(gateway, repo, state);

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        Assert.Equal("sample", repo.Items[0].ContentText);
        Assert.Equal(1, repo.Items[0].CopyCount);
        Assert.Equal(ClipboardItemType.Text, repo.Items[0].Type);
    }

    [Fact]
    public async Task CaptureClipboardTextUseCase_IncrementsDuplicateCopyCount()
    {
        var existing = new ClipboardItem
        {
            ContentText = "sample",
            Hash = ClipboardTextHasher.Compute("sample"),
            CopyCount = 1,
            LastCopiedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Text = "sample" } };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(existing);
        var useCase = new CaptureClipboardTextUseCase(gateway, repo, new ClipboardCaptureState());

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        Assert.Equal(2, repo.Items[0].CopyCount);
    }

    [Fact]
    public async Task CaptureClipboardTextUseCase_SkipsEmptyWhitespaceText()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Text = "   " } };
        var repo = new FakeClipboardItemRepository();
        var useCase = new CaptureClipboardTextUseCase(gateway, repo, new ClipboardCaptureState());

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task CaptureClipboardTextUseCase_SkipsDoNotSaveNextCapture()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Text = "sample" } };
        var repo = new FakeClipboardItemRepository();
        var state = new ClipboardCaptureState();
        state.MarkDoNotSaveNextCapture();
        var useCase = new CaptureClipboardTextUseCase(gateway, repo, state);

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.Items);
        Assert.False(state.ConsumeDoNotSaveNextFlag());
    }

    [Fact]
    public async Task CaptureClipboardTextUseCase_SkipsWhenItemExceedsMaxSize()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Text = "too-big" } };
        var repo = new FakeClipboardItemRepository();
        var options = new ClipboardCleanupOptions { MaxSingleItemBytes = 1 };
        var useCase = new CaptureClipboardTextUseCase(gateway, repo, new ClipboardCaptureState(), options);

        await useCase.ExecuteAsync();

        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task CopyTextItemToClipboardUseCase_WritesTextAndSetsInternalGuard()
    {
        var item = new ClipboardItem { ContentText = "copied", Hash = ClipboardTextHasher.Compute("copied") };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(item);
        var gateway = new FakeClipboardGateway();
        var state = new ClipboardCaptureState();
        var useCase = new CopyTextItemToClipboardUseCase(repo, gateway, state);

        var result = await useCase.ExecuteAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("copied", gateway.WrittenText);
        Assert.True(state.ConsumeInternalClipboardWriteFlag());
    }

    [Fact]
    public async Task CopyTextItemToClipboardUseCase_StoresPreviousClipboardBeforeCopy()
    {
        var item = new ClipboardItem { ContentText = "copied", Hash = ClipboardTextHasher.Compute("copied") };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(item);
        var gateway = new FakeClipboardGateway
        {
            ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Text, Text = "previous" }
        };
        var state = new ClipboardCaptureState();
        var useCase = new CopyTextItemToClipboardUseCase(repo, gateway, state);

        var result = await useCase.ExecuteAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.True(useCase.CanRestorePreviousClipboard);
    }

    [Fact]
    public async Task CopyTextItemToClipboardUseCase_RestoresPreviousTextWithInternalGuard()
    {
        var item = new ClipboardItem { ContentText = "copied", Hash = ClipboardTextHasher.Compute("copied") };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(item);
        var gateway = new FakeClipboardGateway
        {
            ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Text, Text = "previous" }
        };
        var state = new ClipboardCaptureState();
        var useCase = new CopyTextItemToClipboardUseCase(repo, gateway, state);

        await useCase.ExecuteAsync(item.Id);
        _ = state.ConsumeInternalClipboardWriteFlag();
        var result = await useCase.RestorePreviousClipboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("previous", gateway.WrittenText);
        Assert.True(state.ConsumeInternalClipboardWriteFlag());
        Assert.False(useCase.CanRestorePreviousClipboard);
    }

    [Fact]
    public async Task CopyTextItemToClipboardUseCase_RestoresPreviousImageWithInternalGuard()
    {
        var item = new ClipboardItem { ContentText = "copied", Hash = ClipboardTextHasher.Compute("copied") };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(item);
        var imageBytes = new byte[] { 1, 2, 3 };
        var gateway = new FakeClipboardGateway
        {
            ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = imageBytes }
        };
        var state = new ClipboardCaptureState();
        var useCase = new CopyTextItemToClipboardUseCase(repo, gateway, state);

        await useCase.ExecuteAsync(item.Id);
        _ = state.ConsumeInternalClipboardWriteFlag();
        var result = await useCase.RestorePreviousClipboardAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(imageBytes, gateway.WrittenImageBytes);
        Assert.True(state.ConsumeInternalClipboardWriteFlag());
    }

    [Fact]
    public async Task CopyTextItemToClipboardUseCase_WritesImagePathAndSetsInternalGuard()
    {
        var item = new ClipboardItem { ImagePath = "C:/tmp/example.png", Hash = "img-hash" };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(item);
        var gateway = new FakeClipboardGateway();
        var state = new ClipboardCaptureState();
        var useCase = new CopyTextItemToClipboardUseCase(repo, gateway, state);

        var result = await useCase.ExecuteAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(item.ImagePath, gateway.WrittenImagePath);
        Assert.True(state.ConsumeInternalClipboardWriteFlag());
    }

    private sealed class FakeClipboardGateway : IClipboardGateway
    {
        public ClipboardCaptureData? ReadValue { get; set; }
        public string? WrittenText { get; private set; }
        public string? WrittenImagePath { get; private set; }
        public byte[]? WrittenImageBytes { get; private set; }

        public Task<ClipboardCaptureData?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(ReadValue);

        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default)
        {
            WrittenText = text;
            return Task.CompletedTask;
        }

        public Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            WrittenImagePath = imagePath;
            return Task.CompletedTask;
        }

        public Task WriteImageBytesAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            WrittenImageBytes = imageBytes;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public List<ClipboardItem> Items { get; } = [];

        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
        {
            var index = Items.FindIndex(x => x.Id == item.Id);
            Items[index] = item;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.FirstOrDefault(x => x.Hash == hash));

        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ClipboardItem>)Items);

        public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ClipboardItem>)[]);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Count);

        public Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new StorageStats
            {
                TotalItems = Items.Count,
                PinnedItems = Items.Count(x => x.IsPinned),
                ImageItems = Items.Count(x => x.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot),
                ApproxUsageBytes = Items.Sum(x => Math.Max(0, x.SizeBytes))
            });
    }
}
