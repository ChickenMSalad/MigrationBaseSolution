/*
P7.2 SQL Queue Runtime Wiring
MigrationBaseSolution

Purpose:
  Adds operational SQL procedures that turn durable manifest rows into durable queue work,
  support run status transitions, and expose queue/run status summaries for operators.

Notes:
  - This script is additive and idempotent.
  - Requires P7.1 tables from 001_operational_runtime_store.sql.
  - Excel/CSV remain ingestion/export artifacts only.
*/

SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'migration') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA migration AUTHORIZATION dbo;');
END
GO

CREATE OR ALTER PROCEDURE migration.usp_StartMigrationRun
    @RunId UNIQUEIDENTIFIER,
    @StartedAtUtc DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE migration.MigrationRuns
       SET Status = N'Running',
           StartedAtUtc = COALESCE(StartedAtUtc, COALESCE(@StartedAtUtc, SYSUTCDATETIME())),
           UpdatedAtUtc = SYSUTCDATETIME()
     WHERE RunId = @RunId
       AND Status IN (N'Created', N'Pending', N'Queued', N'Validated', N'Ready', N'Running');
END
GO

CREATE OR ALTER PROCEDURE migration.usp_CompleteMigrationRunIfDrained
    @RunId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS
    (
        SELECT 1
          FROM migration.WorkItems
         WHERE RunId = @RunId
           AND Status IN (N'Queued', N'RetryScheduled', N'Running')
    )
    AND EXISTS
    (
        SELECT 1
          FROM migration.MigrationRuns
         WHERE RunId = @RunId
           AND Status IN (N'Running', N'Queued', N'Ready')
    )
    BEGIN
        UPDATE migration.MigrationRuns
           SET Status = CASE
                            WHEN EXISTS (SELECT 1 FROM migration.WorkItems WHERE RunId = @RunId AND Status = N'DeadLetter') THEN N'CompletedWithFailures'
                            WHEN EXISTS (SELECT 1 FROM migration.WorkItems WHERE RunId = @RunId AND Status = N'Failed') THEN N'CompletedWithFailures'
                            ELSE N'Completed'
                        END,
               CompletedAtUtc = COALESCE(CompletedAtUtc, SYSUTCDATETIME()),
               UpdatedAtUtc = SYSUTCDATETIME()
         WHERE RunId = @RunId;
    END
END
GO

CREATE OR ALTER PROCEDURE migration.usp_EnqueueManifestWorkItems
    @RunId UNIQUEIDENTIFIER,
    @WorkType NVARCHAR(128),
    @BatchSize INT = 5000,
    @MaxAttempts INT = 5,
    @Priority INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @BatchSize IS NULL OR @BatchSize <= 0
    BEGIN
        SET @BatchSize = 5000;
    END

    IF @MaxAttempts IS NULL OR @MaxAttempts <= 0
    BEGIN
        SET @MaxAttempts = 5;
    END

    IF @Priority IS NULL
    BEGIN
        SET @Priority = 100;
    END

    ;WITH CandidateRows AS
    (
        SELECT TOP (@BatchSize)
               mr.ManifestRowId,
               mr.RunId,
               mr.PayloadJson,
               mr.Operation,
               mr.SourceExternalId
          FROM migration.ManifestRows mr WITH (READPAST, UPDLOCK, ROWLOCK)
         WHERE mr.RunId = @RunId
           AND mr.ManifestStatus IN (N'Pending', N'Validated', N'Ready')
           AND NOT EXISTS
           (
               SELECT 1
                 FROM migration.WorkItems wi
                WHERE wi.RunId = mr.RunId
                  AND wi.ManifestRowId = mr.ManifestRowId
                  AND wi.WorkType = @WorkType
           )
         ORDER BY mr.ManifestRowId ASC
    )
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
        IdempotencyKey,
        PayloadJson,
        CreatedAtUtc,
        UpdatedAtUtc
    )
    SELECT RunId,
           ManifestRowId,
           @WorkType,
           N'Queued',
           @Priority,
           0,
           @MaxAttempts,
           SYSUTCDATETIME(),
           CONCAT(CONVERT(NVARCHAR(36), RunId), N':', @WorkType, N':', CONVERT(NVARCHAR(32), ManifestRowId)),
           PayloadJson,
           SYSUTCDATETIME(),
           SYSUTCDATETIME()
      FROM CandidateRows;

    DECLARE @Enqueued INT = @@ROWCOUNT;

    UPDATE mr
       SET ManifestStatus = N'Queued',
           UpdatedAtUtc = SYSUTCDATETIME()
      FROM migration.ManifestRows mr
      JOIN migration.WorkItems wi
        ON wi.ManifestRowId = mr.ManifestRowId
       AND wi.RunId = mr.RunId
       AND wi.WorkType = @WorkType
     WHERE mr.RunId = @RunId
       AND mr.ManifestStatus IN (N'Pending', N'Validated', N'Ready');

    SELECT @Enqueued AS EnqueuedCount;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_GetRunOperationalSummary
    @RunId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.RunId,
           r.RunName,
           r.EnvironmentName,
           r.SourceSystem,
           r.TargetSystem,
           r.Status,
           r.IsDryRun,
           r.RequestedAtUtc,
           r.StartedAtUtc,
           r.CompletedAtUtc,
           COUNT(wi.WorkItemId) AS TotalWorkItems,
           SUM(CASE WHEN wi.Status = N'Queued' THEN 1 ELSE 0 END) AS QueuedCount,
           SUM(CASE WHEN wi.Status = N'RetryScheduled' THEN 1 ELSE 0 END) AS RetryScheduledCount,
           SUM(CASE WHEN wi.Status = N'Running' THEN 1 ELSE 0 END) AS RunningCount,
           SUM(CASE WHEN wi.Status = N'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
           SUM(CASE WHEN wi.Status IN (N'Failed', N'DeadLetter') THEN 1 ELSE 0 END) AS FailedCount,
           MAX(wi.UpdatedAtUtc) AS LastWorkItemUpdatedAtUtc
      FROM migration.MigrationRuns r
      LEFT JOIN migration.WorkItems wi
        ON wi.RunId = r.RunId
     WHERE r.RunId = @RunId
     GROUP BY r.RunId,
              r.RunName,
              r.EnvironmentName,
              r.SourceSystem,
              r.TargetSystem,
              r.Status,
              r.IsDryRun,
              r.RequestedAtUtc,
              r.StartedAtUtc,
              r.CompletedAtUtc;
END
GO

CREATE OR ALTER PROCEDURE migration.usp_GetRunnableMigrationRuns
    @MaxRows INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @MaxRows IS NULL OR @MaxRows <= 0
    BEGIN
        SET @MaxRows = 25;
    END

    SELECT TOP (@MaxRows)
           r.RunId,
           r.RunName,
           r.EnvironmentName,
           r.SourceSystem,
           r.TargetSystem,
           r.Status,
           r.IsDryRun,
           r.RequestedAtUtc,
           r.StartedAtUtc,
           r.CompletedAtUtc
      FROM migration.MigrationRuns r
     WHERE r.Status IN (N'Created', N'Pending', N'Queued', N'Validated', N'Ready', N'Running')
     ORDER BY r.RequestedAtUtc ASC;
END
GO
