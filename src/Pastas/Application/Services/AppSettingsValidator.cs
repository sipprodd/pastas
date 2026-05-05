namespace Pastas.Application.Services;

public static class AppSettingsValidator
{
    public const string MaxItemsPositiveMessage = "Max items must be a positive number.";

    public static bool TryParseMaxItems(string? value, out int maxItems, out string? errorMessage)
    {
        if (!int.TryParse(value, out maxItems) || maxItems <= 0)
        {
            errorMessage = MaxItemsPositiveMessage;
            return false;
        }

        errorMessage = null;
        return true;
    }
}
