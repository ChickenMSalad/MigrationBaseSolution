using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationWorkItemRecordMapper
{
    public static MigrationWorkItemRecord Map(
        SqlDataReader reader)
    {
        return new MigrationWorkItemRecord
        {
            WorkItemId = GetLong(reader,"WorkItemId"),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            ManifestRecordId = GetLong(reader,"ManifestRecordId"),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            AttemptCount = reader.GetInt32(reader.GetOrdinal("AttemptCount")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            LockedAt = GetNullableDateTimeOffset(reader, "LockedAt"),
            LockedBy = GetNullableString(reader, "LockedBy"),
            CompletedAt = GetNullableDateTimeOffset(reader, "CompletedAt"),
            FailedAt = GetNullableDateTimeOffset(reader, "FailedAt"),
            LastFailureReason = GetNullableString(reader, "LastFailureReason")
        };
    }

    private static long GetLong(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal)
            ? 0
            : reader.GetInt64(ordinal);
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}
