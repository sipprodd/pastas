namespace Pastas.Application.Services;

public static class TextPreviewBuilder
{
    private const int MaxPreviewLength = 300;

    public static string Build(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (trimmed.Length <= MaxPreviewLength)
        {
            return trimmed;
        }

        return trimmed[..MaxPreviewLength];
    }
}
