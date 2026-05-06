namespace Pastas.Domain.Interfaces;

public interface INotificationService
{
    Task ShowTextCopiedAsync(int totalItems, CancellationToken cancellationToken = default);
    Task ShowImageCopiedAsync(int totalItems, CancellationToken cancellationToken = default);
    Task ShowProtectedTextCopiedAsync(int totalItems, CancellationToken cancellationToken = default);
}
