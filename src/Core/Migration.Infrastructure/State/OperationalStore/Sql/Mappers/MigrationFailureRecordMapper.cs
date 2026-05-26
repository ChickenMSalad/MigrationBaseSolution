using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationFailureRecordMapper
{
    public static MigrationFailureRecord Map(
        SqlDataReader reader)
    {
        return new MigrationFailureRecord
        {
            FailureId = reader.GetGuid(reader.GetOrdinal("FailureId")),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            ManifestRecordId = GetNullableLong(reader, "ManifestRecordId"),
            WorkItemId = GetNullableLong(reader, "WorkItemId"),
            FailureType = reader.GetString(reader.GetOrdinal("FailureType")),
            Message = reader.GetString(reader.GetOrdinal("Message")),
            Details = GetNullableString(reader, "Details"),
            IsRetriable = reader.GetBoolean(reader.GetOrdinal("IsRetriable")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static Guid? GetNullableGuid(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetGuid(ordinal);
    }

    private static long? GetNullableLong(
    SqlDataReader reader,
    string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt64(ordinal);
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
