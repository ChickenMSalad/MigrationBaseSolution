SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT 'P7.9A runtime run parent FK canonicalization starting';

IF OBJECT_ID(N'migration.WorkItems', N'U') IS NULL
BEGIN
    THROW 51090, 'Required table migration.WorkItems does not exist.', 1;
END;

IF OBJECT_ID(N'migration.Runs', N'U') IS NULL
BEGIN
    THROW 51091, 'Required canonical table migration.Runs does not exist.', 1;
END;

IF OBJECT_ID(N'migration.MigrationRuns', N'U') IS NULL
BEGIN
    THROW 51092, 'Legacy table migration.MigrationRuns does not exist; run diagnostics before canonicalization.', 1;
END;

IF COL_LENGTH(N'migration.WorkItems', N'RunId') IS NULL
BEGIN
    THROW 51093, 'Required column migration.WorkItems.RunId does not exist.', 1;
END;

IF COL_LENGTH(N'migration.Runs', N'RunId') IS NULL
BEGIN
    THROW 51094, 'Required column migration.Runs.RunId does not exist.', 1;
END;

BEGIN TRANSACTION;

/*
    Backfill canonical Runs from legacy MigrationRuns so existing WorkItems can survive
    the FK move. The new runtime should write migration.Runs directly after this point.
*/
INSERT INTO migration.Runs
(
    RunId,
    RunKey,
    RunName,
    SourceSystem,
    TargetSystem,
    Status,
    StatusReason,
    EnvironmentName,
    IsDryRun,
    RequestedAtUtc,
    StartedAtUtc,
    CompletedAtUtc,
    CompletionEvaluatedUtc,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    mr.RunId,
    CONCAT(N'legacy-migration-run-', CONVERT(NVARCHAR(36), mr.RunId)) AS RunKey,
    COALESCE(mr.RunName, CONCAT(N'Legacy Migration Run ', CONVERT(NVARCHAR(36), mr.RunId))) AS RunName,
    mr.SourceSystem,
    mr.TargetSystem,
    mr.Status,
    mr.FailureReason AS StatusReason,
    mr.EnvironmentName,
    mr.IsDryRun,
    COALESCE(mr.RequestedAtUtc, mr.CreatedAt) AS RequestedAtUtc,
    COALESCE(mr.StartedAtUtc, mr.StartedAt) AS StartedAtUtc,
    COALESCE(mr.CompletedAtUtc, mr.CompletedAt) AS CompletedAtUtc,
    mr.CompletionEvaluatedUtc,
    COALESCE(mr.RequestedAtUtc, mr.CreatedAt, SYSDATETIMEOFFSET()) AS CreatedAtUtc,
    COALESCE(mr.UpdatedAtUtc, mr.CompletedAtUtc, mr.StartedAtUtc, mr.CreatedAt, SYSDATETIMEOFFSET()) AS UpdatedAtUtc
FROM migration.MigrationRuns mr
LEFT JOIN migration.Runs r ON r.RunId = mr.RunId
WHERE r.RunId IS NULL;

IF EXISTS
(
    SELECT 1
    FROM migration.WorkItems wi
    LEFT JOIN migration.Runs r ON r.RunId = wi.RunId
    WHERE r.RunId IS NULL
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51095, 'Cannot canonicalize WorkItems RunId FK because at least one WorkItems row has no matching migration.Runs row after backfill.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WorkItems_MigrationRuns'
      AND parent_object_id = OBJECT_ID(N'migration.WorkItems')
)
BEGIN
    ALTER TABLE migration.WorkItems DROP CONSTRAINT FK_WorkItems_MigrationRuns;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WorkItems_Runs'
      AND parent_object_id = OBJECT_ID(N'migration.WorkItems')
)
BEGIN
    ALTER TABLE migration.WorkItems WITH CHECK
    ADD CONSTRAINT FK_WorkItems_Runs
        FOREIGN KEY (RunId) REFERENCES migration.Runs(RunId);
END;

IF EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WorkItems_Runs'
      AND parent_object_id = OBJECT_ID(N'migration.WorkItems')
)
BEGIN
    ALTER TABLE migration.WorkItems CHECK CONSTRAINT FK_WorkItems_Runs;
END;

COMMIT TRANSACTION;

PRINT 'P7.9A runtime run parent FK canonicalization completed';
