using Pastas.Application.Services;
using Pastas.Application.UseCases;
using Pastas.Domain.Entities;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;
using Pastas.Shared.Result;

namespace Pastas.UnitTests.Application;

public class ClipboardCaptureCoordinatorTests
{
    [Fact]
    public async Task CaptureAsync_CallsTextAndImageCaptureInOrder()
    {
        var calls = new List<string>();
        var textUseCase = new FakeCaptureClipboardTextUseCase(async () =>
        {
            calls.Add("text");
            await Task.CompletedTask;
        });
        var imageUseCase = new FakeCaptureClipboardImageUseCase(async () =>
        {
            calls.Add("image");
            await Task.CompletedTask;
        });

        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, CreateCleanupService(), null, TimeSpan.Zero);

        await coordinator.CaptureAsync();

        Assert.Equal(["text", "image"], calls);
    }

    [Fact]
    public async Task CaptureAsync_DoesNotThrow_WhenUseCasesFail()
    {
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(() => throw new InvalidOperationException("text fail")),
            new FakeCaptureClipboardImageUseCase(() => throw new InvalidOperationException("image fail")),
            CreateCleanupService(),
            null,
            TimeSpan.Zero);

        var exception = await Record.ExceptionAsync(() => coordinator.CaptureAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task CaptureAsync_PreventsOverlappingRuns()
    {
        var firstStarted = new TaskCompletionSource<bool>();
        var allowFirstToFinish = new TaskCompletionSource<bool>();
        var textCallCount = 0;
        var imageCallCount = 0;

        var textUseCase = new FakeCaptureClipboardTextUseCase(async () =>
        {
            Interlocked.Increment(ref textCallCount);
            firstStarted.TrySetResult(true);
            await allowFirstToFinish.Task;
        });

        var imageUseCase = new FakeCaptureClipboardImageUseCase(() =>
        {
            Interlocked.Increment(ref imageCallCount);
            return Task.CompletedTask;
        });

        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, CreateCleanupService(), null, TimeSpan.Zero);

        var firstCall = coordinator.CaptureAsync();
        await firstStarted.Task;
        await coordinator.CaptureAsync();
        allowFirstToFinish.TrySetResult(true);
        await firstCall;

        Assert.Equal(1, textCallCount);
        Assert.Equal(1, imageCallCount);
    }

    [Fact]
    public async Task CaptureAsync_CanBeCalledMultipleTimesSequentially()
    {
        var textCallCount = 0;
        var imageCallCount = 0;

        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(() =>
            {
                Interlocked.Increment(ref textCallCount);
                return Task.CompletedTask;
            }),
            new FakeCaptureClipboardImageUseCase(() =>
            {
                Interlocked.Increment(ref imageCallCount);
                return Task.CompletedTask;
            }),
            CreateCleanupService(),
            null,
            TimeSpan.Zero);

        await coordinator.CaptureAsync();
        await coordinator.CaptureAsync();
        await coordinator.CaptureAsync();

        Assert.Equal(3, textCallCount);
        Assert.Equal(3, imageCallCount);
    }

    [Fact]
    public async Task CaptureAsync_DoesNotThrow_WhenCleanupFails()
    {
        var coordinator = new ClipboardCaptureCoordinator(
            new FakeCaptureClipboardTextUseCase(() => Task.CompletedTask),
            new FakeCaptureClipboardImageUseCase(() => Task.CompletedTask),
            new ClipboardCleanupService(new ThrowingClipboardItemRepository(), new FakeFileStorage(), new ClipboardCleanupOptions()),
            null,
            TimeSpan.Zero);

        var exception = await Record.ExceptionAsync(() => coordinator.CaptureAsync());

        Assert.Null(exception);
    }

    private static ClipboardCleanupService CreateCleanupService()
        => new(new FakeClipboardItemRepository(), new FakeFileStorage(), new ClipboardCleanupOptions());

    private sealed class FakeCaptureClipboardTextUseCase(Func<Task> action) : ICaptureClipboardTextUseCase
    {
        public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await action();
            return Result.Success();
        }
    }

    private sealed class FakeCaptureClipboardImageUseCase(Func<Task> action) : ICaptureClipboardImageUseCase
    {
        public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await action();
            return Result.Success();
        }
    }

    private class FakeClipboardItemRepository : IClipboardItemRepository
    {
        public Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<ClipboardItem?>(null);
        public Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ClipboardItem>)[]);
        public Task<IReadOnlyList<ClipboardItem>> DeleteByCategoriesAsync(bool includeText, bool includeImages, bool includePinned, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<ClipboardItem>)[]);
        public virtual Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public virtual Task<StorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new StorageStats());
    }

    private sealed class ThrowingClipboardItemRepository : FakeClipboardItemRepository
    {
        public override Task<int> CountAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("cleanup fail");
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SaveImageAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task<string> SaveThumbnailAsync(byte[] bytes, Guid itemId, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
