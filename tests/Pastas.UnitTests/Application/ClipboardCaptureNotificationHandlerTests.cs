using Pastas.Application.Services;
using Pastas.Application.State;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Shared.Result;

namespace Pastas.UnitTests.Application;

public sealed class ClipboardCaptureNotificationHandlerTests
{
    [Fact]
    public async Task HandleClipboardChangedAsync_ShowsClipboardNotification_AfterCapture()
    {
        var repository = new FakeClipboardItemRepository
        {
            TotalCount = 2,
            LatestItem = new ClipboardItem { Type = ClipboardItemType.Image }
        };

        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(),
            new FakeCaptureClipboardImageUseCase(),
            new ClipboardCleanupService(repository, new FakeFileStorage(), new ClipboardCleanupOptions()),
            null,
            TimeSpan.Zero);

        var notificationService = new FakeNotificationService();
        var handler = new ClipboardCaptureNotificationHandler(coordinator, notificationService, repository, new ClipboardCaptureState());

        await handler.HandleClipboardChangedAsync();

        Assert.Equal("Image copied", notificationService.LastNotification?.Title);
        Assert.Equal("Total items: 2", notificationService.LastNotification?.Subtitle);
    }

    [Fact]
    public async Task HandleClipboardChangedAsync_DoesNotThrow_WhenNotificationFails()
    {
        var repository = new FakeClipboardItemRepository();
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(),
            new FakeCaptureClipboardImageUseCase(),
            new ClipboardCleanupService(repository, new FakeFileStorage(), new ClipboardCleanupOptions()),
            null,
            TimeSpan.Zero);

        var notificationService = new ThrowingNotificationService();
        var handler = new ClipboardCaptureNotificationHandler(coordinator, notificationService, repository, new ClipboardCaptureState());

        var exception = await Record.ExceptionAsync(() => handler.HandleClipboardChangedAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleClipboardChangedAsync_SkipsNotification_ForInternalClipboardWrite()
    {
        var repository = new FakeClipboardItemRepository();
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(),
            new FakeCaptureClipboardImageUseCase(),
            new ClipboardCleanupService(repository, new FakeFileStorage(), new ClipboardCleanupOptions()),
            null,
            TimeSpan.Zero);

        var notificationService = new FakeNotificationService();
        var state = new ClipboardCaptureState();
        state.MarkInternalClipboardWrite();
        var handler = new ClipboardCaptureNotificationHandler(coordinator, notificationService, repository, state);

        await handler.HandleClipboardChangedAsync();

        Assert.Null(notificationService.LastNotification);
    }

    private sealed class FakeCaptureClipboardTextUseCase : ICaptureClipboardTextUseCase
    {
        public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }

    private sealed class FakeCaptureClipboardImageUseCase : ICaptureClipboardImageUseCase
    {
        public Task<Result> ExecuteAsync(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
    }

    private sealed class FakeNotificationService : Pastas.Application.Services.INotificationService
    {
        public ClipboardCaptureNotification? LastNotification { get; private set; }
        public void ShowInfo(string message) { }
        public void ShowWarning(string message) { }
        public void ShowClipboardCaptured(ClipboardCaptureNotification notification) => LastNotification = notification;
    }

    private sealed class ThrowingNotificationService : Pastas.Application.Services.INotificationService
    {
        public void ShowInfo(string message) { }
        public void ShowWarning(string message) { }
        public void ShowClipboardCaptured(ClipboardCaptureNotification notification) => throw new InvalidOperationException("notification failed");
    }

    private sealed class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public int TotalCount { get; set; }
        public ClipboardItem? LatestItem { get; set; }

        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ClipboardItem>)(LatestItem is null ? [] : [LatestItem]));
        public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ClipboardItem>)[]);
        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(TotalCount);
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
