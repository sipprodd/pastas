using System.Security.Cryptography;
using System.Text;

namespace Pastas.Application.Services;

public static class ClipboardTextHasher
{
    public static string Compute(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
