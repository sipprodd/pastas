using Microsoft.Data.Sqlite;
using Pastas.Domain.Entities;
using Pastas.Domain.Enums;
using Pastas.Domain.Interfaces;
using Pastas.Domain.ValueObjects;

namespace Pastas.Infrastructure.Storage.SQLite;

public sealed class SqliteClipboardItemRepository : IClipboardItemRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteClipboardItemRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        const string sql = @"INSERT INTO clipboard_items
(id, type, preview_text, content_text, encrypted_content, image_path, thumbnail_path, source_app, source_window_title, hash, is_pinned, is_protected, copy_count, size_bytes, created_at, updated_at, last_copied_at)
VALUES
(@id, @type, @preview_text, @content_text, @encrypted_content, @image_path, @thumbnail_path, @source_app, @source_window_title, @hash, @is_pinned, @is_protected, @copy_count, @size_bytes, @created_at, @updated_at, @last_copied_at);";

        await ExecuteNonQueryForItemAsync(sql, item, cancellationToken);
    }

    public async Task UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        const string sql = @"UPDATE clipboard_items SET
type = @type,
preview_text = @preview_text,
content_text = @content_text,
encrypted_content = @encrypted_content,
image_path = @image_path,
thumbnail_path = @thumbnail_path,
source_app = @source_app,
source_window_title = @source_window_title,
hash = @hash,
is_pinned = @is_pinned,
is_protected = @is_protected,
copy_count = @copy_count,
size_bytes = @size_bytes,
created_at = @created_at,
updated_at = @updated_at,
last_copied_at = @last_copied_at
WHERE id = @id;";

        await ExecuteNonQueryForItemAsync(sql, item, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM clipboard_items WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ClipboardItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM clipboard_items WHERE id = @id;";
        command.Parameters.AddWithValue("@id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadItem(reader) : null;
    }

    public async Task<ClipboardItem?> FindByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM clipboard_items WHERE hash = @hash ORDER BY last_copied_at DESC LIMIT 1;";
        command.Parameters.AddWithValue("@hash", hash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadItem(reader) : null;
    }

    public async Task<IReadOnlyList<ClipboardItem>> SearchAsync(ClipboardSearchQuery query, CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            whereClauses.Add("(preview_text LIKE @query OR source_app LIKE @query OR source_window_title LIKE @query)");
        }

        if (query.Filter == ClipboardFilter.Text)
        {
            whereClauses.Add("type = 'Text'");
        }
        else if (query.Filter == ClipboardFilter.Images)
        {
            whereClauses.Add("type IN ('Image', 'Screenshot')");
        }
        else if (query.Filter == ClipboardFilter.Pinned)
        {
            whereClauses.Add("is_pinned = 1");
        }
        else if (query.Filter == ClipboardFilter.Protected)
        {
            whereClauses.Add("is_protected = 1");
        }

        var where = whereClauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", whereClauses)}";
        var sort = query.SortMode switch
        {
            SortMode.Oldest => "ORDER BY last_copied_at ASC",
            SortMode.MostCopied => "ORDER BY copy_count DESC, last_copied_at DESC",
            _ => "ORDER BY last_copied_at DESC"
        };

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM clipboard_items {where} {sort} LIMIT @limit OFFSET @offset;";

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            command.Parameters.AddWithValue("@query", $"%{query.Query.Trim()}%");
        }

        command.Parameters.AddWithValue("@limit", query.Limit);
        command.Parameters.AddWithValue("@offset", query.Offset);

        var results = new List<ClipboardItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(ReadItem(reader));
        }

        return results;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM clipboard_items;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private async Task ExecuteNonQueryForItemAsync(string sql, ClipboardItem item, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        AddItemParameters(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddItemParameters(SqliteCommand command, ClipboardItem item)
    {
        command.Parameters.AddWithValue("@id", item.Id.ToString());
        command.Parameters.AddWithValue("@type", item.Type.ToString());
        command.Parameters.AddWithValue("@preview_text", (object?)item.PreviewText ?? DBNull.Value);
        command.Parameters.AddWithValue("@content_text", (object?)item.ContentText ?? DBNull.Value);
        command.Parameters.AddWithValue("@encrypted_content", (object?)item.EncryptedContent ?? DBNull.Value);
        command.Parameters.AddWithValue("@image_path", (object?)item.ImagePath ?? DBNull.Value);
        command.Parameters.AddWithValue("@thumbnail_path", (object?)item.ThumbnailPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@source_app", (object?)item.SourceApp ?? DBNull.Value);
        command.Parameters.AddWithValue("@source_window_title", (object?)item.SourceWindowTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("@hash", item.Hash);
        command.Parameters.AddWithValue("@is_pinned", item.IsPinned ? 1 : 0);
        command.Parameters.AddWithValue("@is_protected", item.IsProtected ? 1 : 0);
        command.Parameters.AddWithValue("@copy_count", item.CopyCount);
        command.Parameters.AddWithValue("@size_bytes", item.SizeBytes);
        command.Parameters.AddWithValue("@created_at", item.CreatedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@updated_at", item.UpdatedAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("@last_copied_at", item.LastCopiedAt.ToUniversalTime().ToString("O"));
    }

    private static ClipboardItem ReadItem(SqliteDataReader reader)
    {
        return new ClipboardItem
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Type = Enum.Parse<ClipboardItemType>(reader.GetString(reader.GetOrdinal("type"))),
            PreviewText = ReadNullableString(reader, "preview_text"),
            ContentText = ReadNullableString(reader, "content_text"),
            EncryptedContent = ReadNullableString(reader, "encrypted_content"),
            ImagePath = ReadNullableString(reader, "image_path"),
            ThumbnailPath = ReadNullableString(reader, "thumbnail_path"),
            SourceApp = ReadNullableString(reader, "source_app"),
            SourceWindowTitle = ReadNullableString(reader, "source_window_title"),
            Hash = reader.GetString(reader.GetOrdinal("hash")),
            IsPinned = reader.GetInt32(reader.GetOrdinal("is_pinned")) == 1,
            IsProtected = reader.GetInt32(reader.GetOrdinal("is_protected")) == 1,
            CopyCount = reader.GetInt32(reader.GetOrdinal("copy_count")),
            SizeBytes = reader.GetInt64(reader.GetOrdinal("size_bytes")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))).ToUniversalTime(),
            UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at"))).ToUniversalTime(),
            LastCopiedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("last_copied_at"))).ToUniversalTime()
        };
    }

    private static string? ReadNullableString(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
