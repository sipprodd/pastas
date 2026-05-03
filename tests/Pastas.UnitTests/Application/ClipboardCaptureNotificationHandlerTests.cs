using Pastas.Application.Services;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Shared.Result;

namespace Pastas.UnitTests.Application;

public sealed class ClipboardCaptureNotificationHandlerTests
{
    [Fact]
    public async Task HandleClipboardChangedAsync_ShowsInfoNotification_AfterCapture()
    {
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(),
            new FakeCaptureClipboardImageUseCase(),
            new ClipboardCleanupService(new FakeClipboardItemRepository(), new FakeFileStorage(), new ClipboardCleanupOptions()),
            TimeSpan.Zero);

        var notificationService = new FakeNotificationService();
        var handler = new ClipboardCaptureNotificationHandler(coordinator, notificationService);

        await handler.HandleClipboardChangedAsync();

        Assert.Equal("Clipboard item saved.", notificationService.LastInfoMessage);
    }

    [Fact]
    public async Task HandleClipboardChangedAsync_DoesNotThrow_WhenNotificationFails()
    {
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(),
            new FakeCaptureClipboardImageUseCase(),
            new ClipboardCleanupService(new FakeClipboardItemRepository(), new FakeFileStorage(), new ClipboardCleanupOptions()),
            TimeSpan.Zero);

        var notificationService = new ThrowingNotificationService();
        var handler = new ClipboardCaptureNotificationHandler(coordinator, notificationService);

        var exception = await Record.ExceptionAsync(() => handler.HandleClipboardChangedAsync());

        Assert.Null(exception);
    }

    private sealed class FakeCaptureClipboardTextUseCase : ICaptureClipboardTextUseCase
    {
        public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FakeCaptureClipboardImageUseCase : ICaptureClipboardImageUseCase
    {
        public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FakeNotificationService : Pastas.Application.Services.INotificationService
    {
        public string? LastInfoMessage { get; private set; }

        public void ShowInfo(string message)
        {
            LastInfoMessage = message;
        }

        public void ShowWarning(string message)
        {
        }
    }

    private sealed class ThrowingNotificationService : Pastas.Application.Services.INotificationService
    {
        public void ShowInfo(string message)
        {
            throw new InvalidOperationException("notification failed");
        }

        public void ShowWarning(string message)
        {
        }
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ClipboardItem>)[]);
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
