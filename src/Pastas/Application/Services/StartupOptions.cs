namespace Pastas.Application.Services;

public sealed class StartupOptions
{
    public string? InitialHotkey { get; init; }
    public bool StartMinimized { get; init; }
}
