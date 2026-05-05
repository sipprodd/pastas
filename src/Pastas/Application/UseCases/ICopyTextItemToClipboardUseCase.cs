using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public interface ICopyTextItemToClipboardUseCase
{
    bool CanRestorePreviousClipboard { get; }
    Task<Result> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<Result> RestorePreviousClipboardAsync(CancellationToken cancellationToken = default);
}
