using Pastas.Domain.Enums;

namespace Pastas.Domain.ValueObjects;

public sealed class ClipboardSearchQuery
{
    public string? Query { get; init; }
    public ClipboardFilter Filter { get; init; } = ClipboardFilter.All;
    public SortMode SortMode { get; init; } = SortMode.Recent;
    public int Limit { get; init; } = 50;
    public int Offset { get; init; }
}
