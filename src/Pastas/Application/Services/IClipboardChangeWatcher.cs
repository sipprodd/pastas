namespace Pastas.Application.Services;

public interface IClipboardChangeWatcher
{
    event EventHandler? ClipboardChanged;

    void Start();

    void Stop();
}
