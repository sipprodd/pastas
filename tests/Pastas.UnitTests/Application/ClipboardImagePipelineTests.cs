using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.UnitTests.Application;

public class ClipboardImagePipelineTests
{
    [Fact]
    public void ClipboardImageHasher_ProducesStableHash_ForSameBytes()
    {
        var bytes = new byte[] { 1, 2, 3 };

        var hash1 = ClipboardImageHasher.Compute(bytes);
        var hash2 = ClipboardImageHasher.Compute(bytes);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ClipboardImageHasher_ProducesDifferentHash_ForDifferentBytes()
    {
        var hash1 = ClipboardImageHasher.Compute(new byte[] { 1, 2, 3 });
        var hash2 = ClipboardImageHasher.Compute(new byte[] { 1, 2, 4 });

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_AddsNewImageItem()
    {
        var imageBytes = new byte[] { 7, 8, 9 };
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = imageBytes } };
        var repo = new FakeClipboardItemRepository();
        var storage = new FakeFileStorage();
        var thumbnailBuilder = new FakeThumbnailBuilder();
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, storage, thumbnailBuilder, new ClipboardCaptureState());

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        var item = repo.Items[0];
        Assert.Equal(ClipboardItemType.Image, item.Type);
        Assert.Equal("Image", item.PreviewText);
        Assert.Equal(1, item.CopyCount);
        Assert.Equal(imageBytes.Length, item.SizeBytes);
        Assert.Single(storage.SavedImages);
        Assert.Single(storage.SavedThumbnails);
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_IncrementsDuplicateCopyCount()
    {
        var imageBytes = new byte[] { 7, 8, 9 };
        var hash = ClipboardImageHasher.Compute(imageBytes);
        var existing = new ClipboardItem
        {
            Id = Guid.NewGuid(),
            Type = ClipboardItemType.Image,
            Hash = hash,
            CopyCount = 1,
            ImagePath = "img.png",
            ThumbnailPath = "thumb.png",
            LastCopiedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = imageBytes } };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(existing);
        var storage = new FakeFileStorage();
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, storage, new FakeThumbnailBuilder(), new ClipboardCaptureState());

        var result = await useCase.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        Assert.Equal(2, repo.Items[0].CopyCount);
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_DoesNotResaveFiles_ForDuplicateImage()
    {
        var imageBytes = new byte[] { 7, 8, 9 };
        var existing = new ClipboardItem { Id = Guid.NewGuid(), Type = ClipboardItemType.Image, Hash = ClipboardImageHasher.Compute(imageBytes), CopyCount = 1 };

        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = imageBytes } };
        var repo = new FakeClipboardItemRepository();
        repo.Items.Add(existing);
        var storage = new FakeFileStorage();
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, storage, new FakeThumbnailBuilder(), new ClipboardCaptureState());

        await useCase.ExecuteAsync();

        Assert.Empty(storage.SavedImages);
        Assert.Empty(storage.SavedThumbnails);
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_SkipsWhenNoImageBytes()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = null } };
        var repo = new FakeClipboardItemRepository();
        var storage = new FakeFileStorage();
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, storage, new FakeThumbnailBuilder(), new ClipboardCaptureState());

        await useCase.ExecuteAsync();

        Assert.Empty(repo.Items);
        Assert.Empty(storage.SavedImages);
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_SkipsDoNotSaveNextCapture()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = new byte[] { 1 } } };
        var repo = new FakeClipboardItemRepository();
        var state = new ClipboardCaptureState();
        state.MarkDoNotSaveNextCapture();
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, new FakeFileStorage(), new FakeThumbnailBuilder(), state);

        await useCase.ExecuteAsync();

        Assert.Empty(repo.Items);
        Assert.False(state.ConsumeDoNotSaveNextFlag());
    }

    [Fact]
    public async Task CaptureClipboardImageUseCase_SkipsWhenItemExceedsMaxSize()
    {
        var gateway = new FakeClipboardGateway { ReadValue = new ClipboardCaptureData { Type = ClipboardItemType.Image, ImageBytes = [1, 2, 3] } };
        var repo = new FakeClipboardItemRepository();
        var storage = new FakeFileStorage();
        var options = new ClipboardCleanupOptions { MaxSingleItemBytes = 2 };
        var useCase = new CaptureClipboardImageUseCase(gateway, repo, storage, new FakeThumbnailBuilder(), new ClipboardCaptureState(), options);

        await useCase.ExecuteAsync();

        Assert.Empty(repo.Items);
        Assert.Empty(storage.SavedImages);
    }

    private sealed class FakeThumbnailBuilder : IImageThumbnailBuilder
    {
        public Task<byte[]> BuildAsync(byte[] originalImageBytes, CancellationToken cancellationToken = default)
            => Task.FromResult(new byte[] { 9, 9, 9 });
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public List<(Guid ItemId, byte[] Bytes)> SavedImages { get; } = [];
        public List<(Guid ItemId, byte[] Bytes)> SavedThumbnails { get; } = [];

        public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default)
        {
            SavedImages.Add((itemId, bytes));
            return Task.FromResult($"image-{itemId}.png");
        }

        public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default)
        {
            SavedThumbnails.Add((itemId, bytes));
            return Task.FromResult($"thumb-{itemId}.png");
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeClipboardGateway : IClipboardGateway
    {
        public ClipboardCaptureData? ReadValue { get; set; }

        public Task<ClipboardCaptureData?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(ReadValue);
        public Task WriteTextAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteImageAsync(string imagePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public List<ClipboardItem> Items { get; } = [];

        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) { Items.Add(item); return Task.CompletedTask; }
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
        {
            var index = Items.FindIndex(x => x.Id == item.Id);
            if (index >= 0)
            {
                Items[index] = item;
            }
            else
            {
                Items.Add(item);
            }

            return Task.CompletedTask;
        }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(x => x.Hash == hash));
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ClipboardItem>)Items);
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.Count);
    }
}
