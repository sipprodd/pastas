using Pastas.Application.Services;
using Pastas.Application.UseCases;
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

    private sealed class FakeNotificationService : INotificationService
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

    private sealed class ThrowingNotificationService : INotificationService
    {
        public void ShowInfo(string message)
        {
            throw new InvalidOperationException("notification failed");
        }

        public void ShowWarning(string message)
        {
        }
    }
}
