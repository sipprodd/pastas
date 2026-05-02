using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public sealed class CaptureClipboardImageUseCase
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IImageThumbnailBuilder _imageThumbnailBuilder;
    private readonly ClipboardCaptureState _captureState;

    public CaptureClipboardImageUseCase(
        IClipboardGateway clipboardGateway,
        IClipboardItemRepository clipboardItemRepository,
        IFileStorage fileStorage,
        IImageThumbnailBuilder imageThumbnailBuilder,
        ClipboardCaptureState captureState)
    {
        _clipboardGateway = clipboardGateway;
        _clipboardItemRepository = clipboardItemRepository;
        _fileStorage = fileStorage;
        _imageThumbnailBuilder = imageThumbnailBuilder;
        _captureState = captureState;
    }

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_captureState.ConsumeInternalClipboardWriteFlag() || _captureState.ConsumeDoNotSaveNextFlag())
        {
            return Result.Success();
        }

        var captureData = await _clipboardGateway.ReadAsync(cancellationToken);
        if (captureData?.ImageBytes is null || captureData.ImageBytes.Length == 0)
        {
            return Result.Success();
        }

        var imageBytes = captureData.ImageBytes;
        var hash = ClipboardImageHasher.Compute(imageBytes);
        var now = DateTime.UtcNow;

        var existingItem = await _clipboardItemRepository.FindByHashAsync(hash, cancellationToken);
        if (existingItem is not null)
        {
            var updatedItem = new ClipboardItem
            {
                Id = existingItem.Id,
                Type = existingItem.Type,
                PreviewText = existingItem.PreviewText,
                ContentText = existingItem.ContentText,
                EncryptedContent = existingItem.EncryptedContent,
                ImagePath = existingItem.ImagePath,
                ThumbnailPath = existingItem.ThumbnailPath,
                SourceApp = existingItem.SourceApp,
                SourceWindowTitle = existingItem.SourceWindowTitle,
                Hash = existingItem.Hash,
                IsPinned = existingItem.IsPinned,
                IsProtected = existingItem.IsProtected,
                CopyCount = existingItem.CopyCount + 1,
                SizeBytes = existingItem.SizeBytes,
                CreatedAt = existingItem.CreatedAt,
                UpdatedAt = now,
                LastCopiedAt = now
            };

            await _clipboardItemRepository.UpdateAsync(updatedItem, cancellationToken);
            return Result.Success();
        }

        var itemId = Guid.NewGuid();
        var imagePath = await _fileStorage.SaveImageAsync(imageBytes, itemId, cancellationToken);
        var thumbnailBytes = await _imageThumbnailBuilder.BuildAsync(imageBytes, cancellationToken);
        var thumbnailPath = await _fileStorage.SaveThumbnailAsync(thumbnailBytes, itemId, cancellationToken);

        var item = new ClipboardItem
        {
            Id = itemId,
            Type = ClipboardItemType.Image,
            PreviewText = "Image",
            ImagePath = imagePath,
            ThumbnailPath = thumbnailPath,
            Hash = hash,
            CopyCount = 1,
            SizeBytes = imageBytes.LongLength,
            CreatedAt = now,
            UpdatedAt = now,
            LastCopiedAt = now,
            SourceApp = captureData.SourceApp,
            SourceWindowTitle = captureData.SourceWindowTitle
        };

        await _clipboardItemRepository.AddAsync(item, cancellationToken);
        return Result.Success();
    }
}
