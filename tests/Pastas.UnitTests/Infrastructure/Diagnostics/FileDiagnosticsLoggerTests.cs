using Pastas.Infrastructure.Diagnostics;

namespace Pastas.UnitTests.Infrastructure.Diagnostics;

public sealed class FileDiagnosticsLoggerTests
{
    [Fact]
    public void Info_WritesLineToFile()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "pastas-tests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(tempDirectory, "pastas.log");
        var logger = new FileDiagnosticsLogger(logPath);

        logger.Info("Watcher started.");

        Assert.True(File.Exists(logPath));
        var content = File.ReadAllText(logPath);
        Assert.Contains("[INFO]", content);
        Assert.Contains("Watcher started.", content);
    }

    [Fact]
    public void Logging_WhenPathIsInvalid_DoesNotThrow()
    {
        var logger = new FileDiagnosticsLogger("\0");

        var ex = Record.Exception(() => logger.Error("Failed.", new InvalidOperationException("bad")));

        Assert.Null(ex);
    }
}
