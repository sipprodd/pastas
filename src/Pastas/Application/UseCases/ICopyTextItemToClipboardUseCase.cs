using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public interface ICopyTextItemToClipboardUseCase
{
    Task<Result> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default);
}
