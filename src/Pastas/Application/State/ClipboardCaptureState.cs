namespace Pastas.Application.State;

public sealed class ClipboardCaptureState
{
    private int _internalWriteSkipCount;
    private int _doNotSaveNextSkipCount;

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
}
