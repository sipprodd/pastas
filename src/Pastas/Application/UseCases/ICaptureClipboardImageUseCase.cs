using Pastas.Shared.Result;

namespace Pastas.Application.UseCases;

public interface ICaptureClipboardImageUseCase
{
    Task<Result> ExecuteAsync(CancellationToken cancellationToken = default);
}
