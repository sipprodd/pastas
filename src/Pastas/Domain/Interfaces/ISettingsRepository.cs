using Pastas.Domain.Entities;

namespace Pastas.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
