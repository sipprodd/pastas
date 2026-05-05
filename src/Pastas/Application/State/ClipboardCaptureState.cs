namespace Pastas.Application.State;

public sealed class ClipboardCaptureState
{
    private int _internalWriteSkipCount;
    private int _doNotSaveNextSkipCount;
    private int _isCapturePaused;

    public bool IsCapturePaused => Volatile.Read(ref _isCapturePaused) == 1;

    public void MarkInternalClipboardWrite()
    {
        Interlocked.Exchange(ref _internalWriteSkipCount, 1);
    }

    public bool ConsumeInternalClipboardWriteFlag()
    {
        return Interlocked.Exchange(ref _internalWriteSkipCount, 0) == 1;
    }

    public void MarkDoNotSaveNextCapture()
    {
        Interlocked.Exchange(ref _doNotSaveNextSkipCount, 1);
    }

    public bool ConsumeDoNotSaveNextFlag()
    {
        return Interlocked.Exchange(ref _doNotSaveNextSkipCount, 0) == 1;
    }

    public void PauseCapture()
    {
        Interlocked.Exchange(ref _isCapturePaused, 1);
    }

    public void ResumeCapture()
    {
        Interlocked.Exchange(ref _isCapturePaused, 0);
    }

    public bool ToggleCapturePause()
    {
        while (true)
        {
            var current = Volatile.Read(ref _isCapturePaused);
            var next = current == 1 ? 0 : 1;
            if (Interlocked.CompareExchange(ref _isCapturePaused, next, current) == current)
            {
                return next == 1;
            }
        }
    }
}
