using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationIdentifierMapRecordMapper
{
    public static MigrationIdentifierMapRecord Map(
        SqlDataReader reader)
    {
        return new MigrationIdentifierMapRecord
        {
            IdentifierMapId = reader.GetGuid(reader.GetOrdinal("IdentifierMapId")),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            ManifestRecordId = GetLong(reader, "ManifestRecordId"),
            SourceId = reader.GetString(reader.GetOrdinal("SourceId")),
            TargetId = reader.GetString(reader.GetOrdinal("TargetId")),
            TargetPath = GetNullableString(reader, "TargetPath"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt"))
        };
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

    private static long GetLong(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.GetInt64(ordinal);
    }
}
