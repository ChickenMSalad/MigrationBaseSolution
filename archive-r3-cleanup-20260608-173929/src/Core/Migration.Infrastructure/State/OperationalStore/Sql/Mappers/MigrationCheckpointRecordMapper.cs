using Migration.Application.Models.OperationalStore;
using Microsoft.Data.SqlClient;

namespace Migration.Infrastructure.State.OperationalStore.Sql.Mappers;

internal static class MigrationCheckpointRecordMapper
{
    public static MigrationCheckpointRecord Map(
        SqlDataReader reader)
    {
        return new MigrationCheckpointRecord
        {
            CheckpointId = reader.GetGuid(reader.GetOrdinal("CheckpointId")),
            RunId = reader.GetGuid(reader.GetOrdinal("RunId")),
            CheckpointName = reader.GetString(reader.GetOrdinal("CheckpointName")),
            CheckpointValue = reader.GetString(reader.GetOrdinal("CheckpointValue")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt"))
        };
    }
}
