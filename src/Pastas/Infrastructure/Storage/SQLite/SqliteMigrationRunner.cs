using Microsoft.Data.Sqlite;

namespace Pastas.Infrastructure.Storage.SQLite;

public sealed class SqliteMigrationRunner
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteMigrationRunner(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var migrationSql = await File.ReadAllTextAsync(GetMigrationPath(), cancellationToken);
        await using (var migrationCommand = connection.CreateCommand())
        {
            migrationCommand.CommandText = migrationSql;
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var versionCommand = connection.CreateCommand();
        versionCommand.Transaction = transaction;
        versionCommand.CommandText = @"
            DELETE FROM schema_version;
            INSERT INTO schema_version(version) VALUES (1);";
        await versionCommand.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static string GetMigrationPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Storage", "SQLite", "Migrations", "001_init.sql");
    }
}
