using Microsoft.Data.Sqlite;

namespace Pastas.Infrastructure.Storage.SQLite;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;

    public SqliteConnectionFactory(SqliteDatabasePathProvider pathProvider)
        : this(pathProvider.GetDatabasePath())
    {
    }

    public SqliteConnectionFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);

        await using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
        await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }
}
