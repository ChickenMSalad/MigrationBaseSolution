namespace Migration.Infrastructure.Sql.Records;

public sealed record SqlMigrationProjectRecord(
    Guid ProjectId,
    string ProjectKey,
    string ProjectName,
    string Status,
    string? SettingsJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SqlMigrationRunRecord(
    Guid RunId,
    Guid ProjectId,
    string RunKey,
    string? RunName,
    string SourceSystem,
    string TargetSystem,
    string Status,
    string? StatusReason,
    string? EnvironmentName,
    bool IsDryRun,
    string? CoordinatorOwner,
    DateTimeOffset? CoordinationLeaseExpiresUtc,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SqlMigrationManifestRowRecord(
    long ManifestRowId,
    Guid RunId,
    long SourceRowNumber,
    string SourceExternalId,
    string? SourcePath,
    string? ContentHash,
    string Operation,
    string ManifestStatus,
    string PayloadJson,
    string? ValidationJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SqlMigrationWorkItemRecord(
    long WorkItemId,
    Guid RunId,
    long? ManifestRowId,
    string WorkType,
    string Status,
    int Priority,
    int AttemptCount,
    int MaxAttempts,
    DateTime AvailableAtUtc,
    string? ClaimedBy,
    DateTime? ClaimedAtUtc,
    DateTime? LeaseExpiresAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string IdempotencyKey,
    string PayloadJson,
    string? ResultJson,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string? PartitionKey,
    DateTime? NotBeforeUtc,
    DateTime? LeaseExpiresUtc,
    DateTime? CreatedUtc,
    string? LeaseOwner,
    DateTime? UpdatedUtc,
    string? WorkItemType,
    DateTime? DispatchedAtUtc);

public sealed record SqlMigrationFailureRecord(
    Guid FailureId,
    Guid RunId,
    long? WorkItemId,
    long? ManifestRowId,
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