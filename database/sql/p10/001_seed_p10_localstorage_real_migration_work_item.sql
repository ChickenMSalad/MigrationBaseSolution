:setvar RunId "00000000-0000-0000-0000-000000000000"
:setvar PayloadPath "profiles/jobs/p10-localstorage-real-migration.job.json"

SET NOCOUNT ON;

DECLARE @RunId uniqueidentifier = CONVERT(uniqueidentifier, '$(RunId)');
DECLARE @PayloadJson nvarchar(max) = N'{
  "jobName": "P10LocalStorageRealMigration",
  "sourceType": "LocalStorage",
  "targetType": "LocalStorage",
  "manifestType": "Csv",
  "mappingProfilePath": "profiles/mappings/p10-localstorage-real-migration.mapping.json",
  "dryRun": true
}';

IF @RunId = '00000000-0000-0000-0000-000000000000'
BEGIN
    THROW 51010, 'RunId sqlcmd variable must be supplied.', 1;
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    THROW 51011, 'Required table migration.Runs does not exist. Apply P7.9A canonical runtime schema first.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51012, 'Required table migration.WorkItems does not exist. Apply canonical runtime schema first.', 1;
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
        CONCAT(N'p10-localstorage-', CONVERT(nvarchar(36), @RunId)),
        N'P10 LocalStorage Real Migration',
        N'LocalStorage',
        N'LocalStorage',
        N'Queued',
        N'Azure',
        1,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    );
END;

INSERT INTO migration.WorkItems
(
    RunId,
    WorkType,
    Status,
    AttemptCount,
    MaxAttempts,
    PayloadJson,
    CreatedAtUtc,
    UpdatedAtUtc,
    Priority
)
VALUES
(
    @RunId,
    N'MigrationJobDefinition',
    N'Queued',
    0,
    3,
    @PayloadJson,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    100
);

SELECT TOP (10)
    WorkItemId,
    RunId,
    WorkType,
    Status,
    AttemptCount,
    ClaimedBy,
    CreatedAtUtc,
    UpdatedAtUtc,
    LastErrorMessage
FROM migration.WorkItems
WHERE RunId = @RunId
ORDER BY WorkItemId DESC;
