using Pastas.Application.Services;

namespace Pastas.UnitTests.Application;

public class AppSettingsValidatorTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParseMaxItems_RejectsInvalidValues(string value)
    {
        var parsed = AppSettingsValidator.TryParseMaxItems(value, out _, out var errorMessage);

        Assert.False(parsed);
        Assert.Equal(AppSettingsValidator.MaxItemsPositiveMessage, errorMessage);
    }

    [Fact]
    public void TryParseMaxItems_AcceptsPositiveInteger()
    {
        var parsed = AppSettingsValidator.TryParseMaxItems("3", out var maxItems, out var errorMessage);

        Assert.True(parsed);
        Assert.Equal(3, maxItems);
        Assert.Null(errorMessage);
    }
}
