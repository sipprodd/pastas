using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public interface ICaptureClipboardTextUseCase
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken = default);
}
