using Pastas.Application.State;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public sealed class CopyTextItemToClipboardUseCase : ICopyTextItemToClipboardUseCase
{
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly IClipboardGateway _clipboardGateway;
    private readonly ClipboardCaptureState _captureState;
    private ClipboardCaptureData? _previousClipboard;

    public CopyTextItemToClipboardUseCase(
        IClipboardItemRepository clipboardItemRepository,
        IClipboardGateway clipboardGateway,
        ClipboardCaptureState captureState)
    {
        _clipboardItemRepository = clipboardItemRepository;
        _clipboardGateway = clipboardGateway;
        _captureState = captureState;
    }

    public bool CanRestorePreviousClipboard => _previousClipboard is not null;

    public async Task<Result> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _clipboardItemRepository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure(new Error("clipboard.item.not_found", "Clipboard item not found."));
        }

        _previousClipboard = await ReadPreviousClipboardAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(item.ContentText))
        {
            _captureState.MarkInternalClipboardWrite();
            await _clipboardGateway.WriteTextAsync(item.ContentText, cancellationToken);

            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(item.ImagePath))
        {
            return Result.Failure(new Error("clipboard.item.unsupported", "Clipboard item cannot be copied."));
        }

        _captureState.MarkInternalClipboardWrite();
        await _clipboardGateway.WriteImageAsync(item.ImagePath, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestorePreviousClipboardAsync(CancellationToken cancellationToken = default)
    {
        var previous = _previousClipboard;
        if (previous is null)
        {
            return Result.Failure(new Error("clipboard.previous.not_found", "Previous clipboard is not available."));
        }

        if (previous.Type == ClipboardItemType.Text && !string.IsNullOrEmpty(previous.Text))
        {
            _captureState.MarkInternalClipboardWrite();
            await _clipboardGateway.WriteTextAsync(previous.Text, cancellationToken);
            _previousClipboard = null;
            return Result.Success();
        }

        if (previous.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot
            && previous.ImageBytes is { Length: > 0 } imageBytes)
        {
            _captureState.MarkInternalClipboardWrite();
            await _clipboardGateway.WriteImageBytesAsync(imageBytes, cancellationToken);
            _previousClipboard = null;
            return Result.Success();
        }

        return Result.Failure(new Error("clipboard.previous.unsupported", "Previous clipboard cannot be restored."));
    }

    private async Task<ClipboardCaptureData?> ReadPreviousClipboardAsync(CancellationToken cancellationToken)
    {
        try
        {
            var previous = await _clipboardGateway.ReadAsync(cancellationToken);
            if (previous?.Type == ClipboardItemType.Text && !string.IsNullOrEmpty(previous.Text))
            {
                return previous;
            }

            if (previous?.Type is ClipboardItemType.Image or ClipboardItemType.Screenshot
                && previous.ImageBytes is { Length: > 0 })
            {
                return previous;
            }
        }
        catch
        {
            // Copying should still work if the current clipboard cannot be read.
        }

        return null;
    }
}
