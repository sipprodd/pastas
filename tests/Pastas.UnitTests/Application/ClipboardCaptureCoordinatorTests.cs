using Pastas.Application.Services;
using Pastas.Application.UseCases;
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

        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, TimeSpan.Zero);

        await coordinator.CaptureAsync();

        Assert.Equal(["text", "image"], calls);
    }

    [Fact]
    public async Task CaptureAsync_DoesNotThrow_WhenUseCasesFail()
    {
        var textUseCase = new FakeCaptureClipboardTextUseCase(() => throw new InvalidOperationException("text fail"));
        var imageUseCase = new FakeCaptureClipboardImageUseCase(() => throw new InvalidOperationException("image fail"));
        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, TimeSpan.Zero);

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

        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, TimeSpan.Zero);

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

        var textUseCase = new FakeCaptureClipboardTextUseCase(() =>
        {
            Interlocked.Increment(ref textCallCount);
            return Task.CompletedTask;
        });

        var imageUseCase = new FakeCaptureClipboardImageUseCase(() =>
        {
            Interlocked.Increment(ref imageCallCount);
            return Task.CompletedTask;
        });

        var coordinator = new ClipboardCaptureCoordinator(textUseCase, imageUseCase, TimeSpan.Zero);

        await coordinator.CaptureAsync();
        await coordinator.CaptureAsync();
        await coordinator.CaptureAsync();

        Assert.Equal(3, textCallCount);
        Assert.Equal(3, imageCallCount);
    }

    private sealed class FakeCaptureClipboardTextUseCase : ICaptureClipboardTextUseCase
    {
        private readonly Func<Task> _action;

        public FakeCaptureClipboardTextUseCase(Func<Task> action)
        {
            _action = action;
        }

        public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _action();
            return Result.Success();
        }
    }

    private sealed class FakeCaptureClipboardImageUseCase : ICaptureClipboardImageUseCase
    {
        private readonly Func<Task> _action;

        public FakeCaptureClipboardImageUseCase(Func<Task> action)
        {
            _action = action;
        }

        public async Task<Result> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _action();
            return Result.Success();
        }
    }
}
