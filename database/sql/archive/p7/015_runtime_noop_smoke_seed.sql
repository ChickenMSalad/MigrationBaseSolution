/*
P7.9D Runtime NoOp Smoke Seed

Required sqlcmd variables:
  RunId

This script seeds both run parent tables while WorkItems.RunId FK canonicalization is being rolled forward.
It then inserts one canonical RuntimeSmoke MigrationJobDefinition work item into migration.WorkItems.
*/

SET NOCOUNT ON;

DECLARE @RunId uniqueidentifier = TRY_CONVERT(uniqueidentifier, N'$(RunId)');

IF @RunId IS NULL
BEGIN
    THROW 51010, 'RunId sqlcmd variable is required and must be a uniqueidentifier.', 1;
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    THROW 51011, 'Required table migration.Runs does not exist.', 1;
END;

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NULL
BEGIN
    THROW 51012, 'Required compatibility table migration.MigrationRuns does not exist.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51013, 'Required table migration.WorkItems does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.Runs WHERE RunId = @RunId)
BEGIN
    INSERT INTO migration.Runs
    (
        RunId,
        RunKey,
        RunName,
        SourceSystem,
        TargetSystem,
        Status,
        EnvironmentName,
        IsDryRun,
        RequestedAtUtc,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        @RunId,
        CONCAT(N'runtime-noop-smoke-', CONVERT(nvarchar(36), @RunId)),
        N'Runtime NoOp Smoke',
        N'RuntimeSmoke',
        N'RuntimeSmoke',
        N'Queued',
        N'Azure',
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM migration.MigrationRuns WHERE RunId = @RunId)
BEGIN
    INSERT INTO migration.MigrationRuns
    (
        RunId,
        SourceSystem,
        TargetSystem,
        Status,
        CreatedAt,
        RunName,
        EnvironmentName,
        IsDryRun,
        RequestedAtUtc,
        UpdatedAtUtc
    )
    VALUES
    (
        @RunId,
        N'RuntimeSmoke',
        N'RuntimeSmoke',
        N'Queued',
        SYSDATETIMEOFFSET(),
        N'Runtime NoOp Smoke',
        N'Azure',
        1,
        SYSDATETIMEOFFSET(),
        SYSDATETIMEOFFSET()
    );
END;

DECLARE @PayloadJson nvarchar(max) = N'{
  "jobName": "RuntimeSmoke",
  "sourceType": "RuntimeSmoke",
  "targetType": "RuntimeSmoke",
  "manifestType": "RuntimeSmoke",
  "mappingProfilePath": "runtime-smoke.mapping.json",
  "dryRun": true
}';

INSERT INTO migration.WorkItems
(
    RunId,
    ManifestRowId,
    WorkType,
    Status,
    Priority,
    AttemptCount,
    MaxAttempts,
    AvailableAtUtc,
    PayloadJson,
    CreatedAtUtc,
    UpdatedAtUtc,
    PartitionKey,
    NotBeforeUtc,
    WorkItemType,
    CreatedUtc,
    UpdatedUtc
)
VALUES
(
    @RunId,
    NULL,
    N'MigrationJobDefinition',
    N'Queued',
    100,
    0,
    3,
    SYSUTCDATETIME(),
    @PayloadJson,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    N'RuntimeSmoke',
    SYSUTCDATETIME(),
    N'MigrationJobDefinition',
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
);

SELECT TOP (10)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CompletedAtUtc,
    LastErrorMessage,
    CreatedAtUtc
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY WorkItemId DESC;
