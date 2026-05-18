using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationRunRecordMapper
{
    public static MigrationRunRecord Map(
        SqlDataReader reader)
    {
        return new MigrationRunRecord
        {
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            SourceSystem = reader.GetString(reader.GetOrdinal("SourceSystem")),
            TargetSystem = reader.GetString(reader.GetOrdinal("TargetSystem")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            StartedAt = GetNullableDateTimeOffset(reader, "StartedAt"),
            CompletedAt = GetNullableDateTimeOffset(reader, "CompletedAt"),
            FailedAt = GetNullableDateTimeOffset(reader, "FailedAt"),
            FailureReason = GetNullableString(reader, "FailureReason")
        };
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

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }
}
