using System.IO;

namespace Pastas.Infrastructure.Storage.SQLite;

public sealed class SqliteDatabasePathProvider
{
    private const string AppFolderName = "Pastas";
    private const string DatabaseFileName = "pastas.db";

    public string GetDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, AppFolderName, DatabaseFileName);
    }
}
