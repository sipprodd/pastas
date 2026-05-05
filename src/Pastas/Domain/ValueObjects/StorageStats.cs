namespace Pastas.Domain.ValueObjects;

public sealed class StorageStats
{
    public int TotalItems { get; init; }
    public int PinnedItems { get; init; }
    public int ImageItems { get; init; }
    public long ApproxUsageBytes { get; init; }
}
