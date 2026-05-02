using System.Text;
using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public sealed class CaptureClipboardTextUseCase : ICaptureClipboardTextUseCase
{
    private readonly IClipboardGateway _clipboardGateway;
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly ClipboardCaptureState _captureState;

    public CaptureClipboardTextUseCase(
        IClipboardGateway clipboardGateway,
        IClipboardItemRepository clipboardItemRepository,
        ClipboardCaptureState captureState)
    {
        _clipboardGateway = clipboardGateway;
        _clipboardItemRepository = clipboardItemRepository;
        _captureState = captureState;
    }

    public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_captureState.ConsumeInternalClipboardWriteFlag())
        {
            return Result.Success();
        }

        if (_captureState.ConsumeDoNotSaveNextFlag())
        {
            return Result.Success();
        }

        var captureData = await _clipboardGateway.ReadAsync(cancellationToken);
        var text = captureData?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Success();
        }

        var hash = ClipboardTextHasher.Compute(text);
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

        var newItem = new ClipboardItem
        {
            Type = ClipboardItemType.Text,
            ContentText = text,
            PreviewText = TextPreviewBuilder.Build(text),
            Hash = hash,
            CopyCount = 1,
            SizeBytes = Encoding.UTF8.GetByteCount(text),
            CreatedAt = now,
            UpdatedAt = now,
            LastCopiedAt = now,
            SourceApp = captureData?.SourceApp,
            SourceWindowTitle = captureData?.SourceWindowTitle
        };

        await _clipboardItemRepository.AddAsync(newItem, cancellationToken);
        return Result.Success();
    }
}
