namespace Migration.Infrastructure.Sql.Records;

public sealed record SqlMigrationProjectRecord(
    Guid ProjectId,
    string ProjectKey,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SqlMigrationRunRecord(
    Guid RunId,
    Guid ProjectId,
    string RunKey,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record SqlMigrationManifestRowRecord(
    Guid ManifestRowId,
    Guid RunId,
    long RowNumber,
    string SourceIdentifier,
    string? SourceUri,
    string PayloadJson,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SqlMigrationWorkItemRecord(
    Guid WorkItemId,
    Guid RunId,
    Guid? ManifestRowId,
    string WorkItemType,
    string Status,
    int AttemptCount,
    DateTimeOffset? AvailableAtUtc,
    DateTimeOffset? LeasedUntilUtc,
    string? LeaseOwner,
    string PayloadJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SqlMigrationFailureRecord(
    Guid FailureId,
    Guid RunId,
    Guid? WorkItemId,
    Guid? ManifestRowId,
    string FailureType,
    string FailureCode,
    string Message,
    string? DetailsJson,
    DateTimeOffset CreatedAtUtc);

public sealed record SqlMigrationCheckpointRecord(
    Guid CheckpointId,
    Guid RunId,
    string CheckpointName,
    string CheckpointValue,
    string? PayloadJson,
    DateTimeOffset CreatedAtUtc);

public sealed record SqlMigrationAssetMappingRecord(
    Guid AssetMappingId,
    Guid RunId,
    string SourceSystem,
    string SourceIdentifier,
    string TargetSystem,
    string TargetIdentifier,
    string? PayloadJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
