namespace Pastas.Application.Services;

public sealed class ClipboardCleanupOptions
{
    public int MaxItems { get; init; } = 200;
    public long MaxSingleItemBytes { get; init; } = 25L * 1024L * 1024L;
}
