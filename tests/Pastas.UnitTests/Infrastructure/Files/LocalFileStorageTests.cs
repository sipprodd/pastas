using Pastas.Infrastructure.Files;

namespace Pastas.UnitTests.Infrastructure.Files;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "pastas-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveImageAsync_SavesImageFileToConfiguredPath()
    {
        Directory.CreateDirectory(_tempDir);
        var storage = new LocalFileStorage(_tempDir);
        var itemId = Guid.NewGuid();
        var bytes = new byte[] { 1, 2, 3, 4 };

        var path = await storage.SaveImageAsync(bytes, itemId);

        Assert.True(File.Exists(path));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        Assert.Equal(Path.Combine(_tempDir, "images", $"{itemId}.png"), path);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrow_WhenFileIsMissing()
    {
        var storage = new LocalFileStorage(_tempDir);
        var missingPath = Path.Combine(_tempDir, "missing.png");

        await storage.DeleteAsync(missingPath);

        Assert.False(File.Exists(missingPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
