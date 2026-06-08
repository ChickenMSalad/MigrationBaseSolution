using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationManifestRecordMapper
{
    public static MigrationManifestRecord Map(
        SqlDataReader reader)
    {
        return new MigrationManifestRecord
        {
            ManifestRecordId = GetLong(reader,"ManifestRecordId"),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            SequenceNumber = reader.GetInt64(reader.GetOrdinal("SequenceNumber")),
            SourceId = reader.GetString(reader.GetOrdinal("SourceId")),
            SourcePath = GetNullableString(reader, "SourcePath"),
            SourceName = GetNullableString(reader, "SourceName"),
            ContentType = GetNullableString(reader, "ContentType"),
            ContentLength = GetNullableInt64(reader, "ContentLength"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = GetNullableDateTimeOffset(reader, "UpdatedAt")
        };
    }

    private static long GetLong(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.GetInt64(ordinal);

    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static long? GetNullableInt64(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}
