using Pastas.Application.State;
using Pastas.Domain.Interfaces;
using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public sealed class CopyTextItemToClipboardUseCase : ICopyTextItemToClipboardUseCase
{
    private readonly IClipboardItemRepository _clipboardItemRepository;
    private readonly IClipboardGateway _clipboardGateway;
    private readonly ClipboardCaptureState _captureState;

    public CopyTextItemToClipboardUseCase(
        IClipboardItemRepository clipboardItemRepository,
        IClipboardGateway clipboardGateway,
        ClipboardCaptureState captureState)
    {
        _clipboardItemRepository = clipboardItemRepository;
        _clipboardGateway = clipboardGateway;
        _captureState = captureState;
    }

    public async Task<Result> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _clipboardItemRepository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
        {
            return Result.Failure(new Error("clipboard.item.not_found", "Clipboard item not found."));
        }

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
}
