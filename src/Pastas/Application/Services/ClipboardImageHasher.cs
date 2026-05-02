using System.Security.Cryptography;

namespace Pastas.Application.Services;

public static class ClipboardImageHasher
{
    public static string Compute(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
