using Pastas.Domain.Enums;
using Pastas.Domain.ValueObjects;

namespace Pastas.UnitTests.Domain;

public class ClipboardSearchQueryTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        var query = new ClipboardSearchQuery();

        Assert.Null(query.Query);
        Assert.Equal(ClipboardFilter.All, query.Filter);
        Assert.Equal(SortMode.Recent, query.SortMode);
        Assert.Equal(50, query.Limit);
        Assert.Equal(0, query.Offset);
    }
}
