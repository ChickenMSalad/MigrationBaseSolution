/*
P7.8D runtime smoke work item seed template.

This file documents the SQL shape used by tools/runtime/Invoke-RuntimeSmokeEnqueue.ps1.
The PowerShell script reads config-samples/runtime-smoke-job-definition.sample.json,
escapes it safely, writes a temporary SQL file, and executes that file with sqlcmd.

Do not insert into legacy MigrationWorkItems here. The canonical runtime queue table is migration.WorkItems.
*/

DECLARE @RunId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @PayloadJson nvarchar(max) = N'{"jobName":"RuntimeSmoke","sourceType":"LocalStorage","targetType":"LocalStorage","manifestType":"Csv","mappingProfilePath":"smoke.json","dryRun":true}';

IF @RunId IS NULL
BEGIN
    THROW 51000, 'RunId is required.', 1;
END;

IF @PayloadJson IS NULL OR ISJSON(@PayloadJson) <> 1
BEGIN
    THROW 51001, 'Payload JSON is required and must be valid JSON.', 1;
END;

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51002, 'Required table migration.WorkItems does not exist.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM migration.MigrationRuns WHERE RunId = @RunId)
   AND NOT EXISTS (SELECT 1 FROM migration.Runs WHERE RunId = @RunId)
BEGIN
    THROW 51003, 'RunId does not exist in migration.MigrationRuns or migration.Runs. Seed/create the run before enqueueing smoke work.', 1;
END;

INSERT INTO migration.WorkItems
(
    RunId,
    WorkType,
    Status,
    Priority,
    AttemptCount,
    MaxAttempts,
    PayloadJson,
    CreatedAtUtc,
    UpdatedAtUtc
)
VALUES
(
    @RunId,
    N'MigrationJobDefinition',
    N'Queued',
    100,
    0,
    3,
    @PayloadJson,
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
ORDER BY WorkItemId DESC;
